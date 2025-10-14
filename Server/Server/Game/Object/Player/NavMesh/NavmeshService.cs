using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

public sealed class NavmeshService
{
    public static NavmeshService Instance { get; } = new NavmeshService();
    private TriCache _cache;
    private ITriAccel _accel;

    public void Init(TriCache cache, ITriAccel accel)
    {
        _cache = cache;
        _accel = accel;
        // accel.Build는 외부에서 한 뒤 넘겨도 되고 여기서 호출해도 됨.
    }

    // Clamp: 후보 tris만 받아 정밀 검사해 최종 투영
    public Vector3 ClampPointToNavmesh(Vector3 p)
    {
        float best = float.MaxValue;
        var bestP = p;
        foreach (var t in _accel.QueryCandidatesNearPoint(p, 0.5f))
        {
            var tri = _cache.Tris[t];
            var q = ProjectPointToTri(p, tri); // 구현체: 평면투영→inside? → 아니면 최근접 엣지/버텍스
            float d = Vector3.DistanceSquared(p, q);
            if (d < best)
            { best = d; bestP = q; }
        }
        return bestP;
    }

    // Sweep: 후보 tris만 받아 정밀 충돌 검사
    public bool SweepCapsule(Vector3 a, Vector3 b, float r,
                             out Vector3 hit, out Vector3 nrm)
    {
        hit = b;
        nrm = new Vector3(0, 1, 0);
        float bestTHit = 2f;
        bool ok = false;
        foreach (var t in _accel.QueryCandidatesAlongSegment(a, b, r))
        {
            var tri = _cache.Tris[t];
            if (IntersectSegmentCapsuleTri(a, b, r, tri, out var hp, out var hn, out var th))
            {
                if (th < bestTHit)
                { bestTHit = th; hit = hp; nrm = hn; ok = true; }
            }
        }
        return ok;
    }

    // TriCache.Tri : v0,v1,v2, normal, d, aabbMin/Max
    // plane eq: tri.normal · x + tri.d = 0
    private static Vector3 ProjectPointToTri(Vector3 p, TriCache.Tri tri)
    {
        // 1) 평면 투영
        float dist = Vector3.Dot(tri.normal, p) + tri.d;
        Vector3 proj = p - tri.normal * dist;

        // 2) 바리센트릭으로 내부성 검사
        if (PointInTriangleBarycentric(proj, tri.v0, tri.v1, tri.v2))
            return proj;

        // 3) 내부가 아니면, 엣지/버텍스 최근접점 중 최단
        Vector3 c01 = ClosestPointOnSegment(proj, tri.v0, tri.v1);
        Vector3 c12 = ClosestPointOnSegment(proj, tri.v1, tri.v2);
        Vector3 c20 = ClosestPointOnSegment(proj, tri.v2, tri.v0);

        float d01 = Vector3.DistanceSquared(proj, c01);
        float d12 = Vector3.DistanceSquared(proj, c12);
        float d20 = Vector3.DistanceSquared(proj, c20);

        Vector3 best = c01;
        float bestD = d01;
        if (d12 < bestD)
        { best = c12; bestD = d12; }
        if (d20 < bestD)
        { best = c20; /*bestD = d20;*/ }

        return best;
    }

    private static bool IntersectSegmentCapsuleTri(
    Vector3 a, Vector3 b, float r,
    TriCache.Tri tri,
    out Vector3 hitPoint, out Vector3 hitNormal, out float tHit)
    {
        hitPoint = b;
        hitNormal = tri.normal;
        tHit = 2f;

        // 빠른 AABB 전검사(세그먼트 + 반경)
        Vector3 segMin = new Vector3(MathF.Min(a.X, b.X) - r, MathF.Min(a.Y, b.Y) - r, MathF.Min(a.Z, b.Z) - r);
        Vector3 segMax = new Vector3(MathF.Max(a.X, b.X) + r, MathF.Max(a.Y, b.Y) + r, MathF.Max(a.Z, b.Z) + r);
        if (!AabbOverlap(segMin, segMax, tri.aabbMin, tri.aabbMax))
            return false;

        // coarse: 균등 샘플로 최초 교차 구간 찾기
        const int STEPS = 16;
        Vector3 v = b - a;
        float prevT = 0f;
        float prevVal = DistanceToTriangleSquared(a, tri) - r * r;

        bool found = false;
        float t0 = 0f, t1 = 0f;

        for (int i = 1; i <= STEPS; i++)
        {
            float t = (float)i / STEPS;
            Vector3 p = a + v * t;
            float val = DistanceToTriangleSquared(p, tri) - r * r;

            if (val <= 0f && prevVal > 0f)
            {
                // 처음 안으로 들어온 구간 [prevT, t]
                t0 = prevT;
                t1 = t;
                found = true;
                break;
            }
            prevT = t;
            prevVal = val;
        }

        if (!found)
            return false;

        // refine: 이분 탐색으로 tHit 정밀화
        float lo = t0, hi = t1;
        for (int it = 0; it < 8; it++)
        {
            float mid = 0.5f * (lo + hi);
            Vector3 pm = a + v * mid;
            float val = DistanceToTriangleSquared(pm, tri) - r * r;
            if (val <= 0f)
                hi = mid;
            else
                lo = mid;
        }

        tHit = hi;
        Vector3 pc = a + v * tHit;             // 충돌 시점의 구 중심
        Vector3 q = ClosestPointOnTriangle(pc, tri, out Vector3 featureN); // 최근접 삼각형점 & 대략적 노멀
        hitPoint = q;

        // 노멀: hp -> 구 중심 (너무 짧으면 tri.normal 참고)
        Vector3 n = pc - q;
        if (n.LengthSquared() < 1e-12f)
            n = featureN; // 안전
        else
            n = Vector3.Normalize(n);

        // 뒤집히지 않게 삼각형 노멀과 방향 정리(원하는 규칙으로)
        if (Vector3.Dot(n, tri.normal) < 0)
            n = -n;
        hitNormal = n;

        return true;
    }

    #region Helper
    private static bool PointInTriangleBarycentric(in Vector3 p, in Vector3 a, in Vector3 b, in Vector3 c)
    {
        // 평면상 바리센트릭 (Möller–Trumbore 변형)
        Vector3 v0 = b - a, v1 = c - a, v2 = p - a;
        float d00 = Vector3.Dot(v0, v0);
        float d01 = Vector3.Dot(v0, v1);
        float d11 = Vector3.Dot(v1, v1);
        float d20 = Vector3.Dot(v2, v0);
        float d21 = Vector3.Dot(v2, v1);
        float denom = d00 * d11 - d01 * d01;
        if (denom <= 1e-20f)
            return false; // 퇴화

        float v = (d11 * d20 - d01 * d21) / denom;
        float w = (d00 * d21 - d01 * d20) / denom;
        float u = 1.0f - v - w;

        const float EPS = -1e-5f; // 경계 살짝 허용
        return (u >= EPS && v >= EPS && w >= EPS);
    }

    private static Vector3 ClosestPointOnSegment(in Vector3 p, in Vector3 a, in Vector3 b)
    {
        Vector3 ab = b - a;
        float ab2 = Vector3.Dot(ab, ab);
        if (ab2 <= 1e-20f)
            return a;
        float t = Vector3.Dot(p - a, ab) / ab2;
        if (t <= 0)
            return a;
        if (t >= 1)
            return b;
        return a + ab * t;
    }

    private static float DistanceToTriangleSquared(in Vector3 p, in TriCache.Tri tri)
    {
        Vector3 q = ClosestPointOnTriangle(p, tri, out _);
        return Vector3.DistanceSquared(p, q);
    }

    private static Vector3 ClosestPointOnTriangle(in Vector3 p, in TriCache.Tri tri, out Vector3 approxNormal)
    {
        // 평면 투영 → 내부면 OK
        float dist = Vector3.Dot(tri.normal, p) + tri.d;
        Vector3 proj = p - tri.normal * dist;
        if (PointInTriangleBarycentric(proj, tri.v0, tri.v1, tri.v2))
        {
            approxNormal = tri.normal;
            return proj;
        }

        // 아니면 엣지/버텍스 최근접
        Vector3 c01 = ClosestPointOnSegment(proj, tri.v0, tri.v1);
        Vector3 c12 = ClosestPointOnSegment(proj, tri.v1, tri.v2);
        Vector3 c20 = ClosestPointOnSegment(proj, tri.v2, tri.v0);

        float d01 = Vector3.DistanceSquared(proj, c01);
        float d12 = Vector3.DistanceSquared(proj, c12);
        float d20 = Vector3.DistanceSquared(proj, c20);

        if (d01 <= d12 && d01 <= d20)
        { approxNormal = SafeEdgeNormal(tri, tri.v0, tri.v1); return c01; }
        if (d12 <= d01 && d12 <= d20)
        { approxNormal = SafeEdgeNormal(tri, tri.v1, tri.v2); return c12; }
        approxNormal = SafeEdgeNormal(tri, tri.v2, tri.v0);
        return c20;
    }

    private static Vector3 SafeEdgeNormal(in TriCache.Tri tri, in Vector3 a, in Vector3 b)
    {
        // 엣지 방향 × world up, tri.normal 쪽으로 정리
        Vector3 edge = Vector3.Normalize(b - a);
        // tri.normal과 수직인 어떤 벡터를 써도 되지만, tri.normal로 fallback
        Vector3 n = Vector3.Normalize(Vector3.Cross(edge, Vector3.UnitY));
        if (n.LengthSquared() < 1e-6f)
            n = tri.normal;
        if (Vector3.Dot(n, tri.normal) < 0)
            n = -n;
        return n;
    }

    private static bool AabbOverlap(in Vector3 aMin, in Vector3 aMax, in Vector3 bMin, in Vector3 bMax)
    {
        return !(aMin.X > bMax.X || aMax.X < bMin.X ||
                 aMin.Y > bMax.Y || aMax.Y < bMin.Y ||
                 aMin.Z > bMax.Z || aMax.Z < bMin.Z);
    }
    #endregion
}
