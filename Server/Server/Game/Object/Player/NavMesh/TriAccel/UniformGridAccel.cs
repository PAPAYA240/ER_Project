using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

public sealed class UniformGridAccel : ITriAccel
{
    private TriCache _cache;
    private Vector3 _min, _max;
    private float _cell;                // 셀 크기 (예: 0.5f)
    private int _nx, _nz;               // XZ 기준 셀 수
    private List<int>[] _buckets;       // 각 셀에 들어가는 tri 인덱스
    private const float EPS = 1e-5f;

    public UniformGridAccel(float cell = 0.5f) { _cell = MathF.Max(cell, 1e-3f); }

    public void Build(TriCache cache, Vector3 min, Vector3 max)
    {
        _cache = cache;
        _min = min;
        _max = max;
        _nx = Math.Max(1, (int)MathF.Ceiling((max.X - min.X) / _cell));
        _nz = Math.Max(1, (int)MathF.Ceiling((max.Z - min.Z) / _cell));
        _buckets = new List<int>[_nx * _nz];
        for (int i = 0; i < _buckets.Length; i++)
            _buckets[i] = new List<int>(8);

        // 삼각형 AABB를 셀 그리드에 라스터라이즈
        for (int t = 0; t < _cache.Tris.Length; t++)
        {
            var tri = _cache.Tris[t];
            (int ix0, int iz0) = ToCell(tri.aabbMin);
            (int ix1, int iz1) = ToCell(tri.aabbMax);
            for (int iz = Clamp0(iz0, _nz - 1); iz <= Clamp0(iz1, _nz - 1); iz++)
                for (int ix = Clamp0(ix0, _nx - 1); ix <= Clamp0(ix1, _nx - 1); ix++)
                    _buckets[iz * _nx + ix].Add(t);
        }
    }

    public IEnumerable<int> QueryCandidatesNearPoint(Vector3 p, float radius = 0f)
    {
        var hs = new HashSet<int>();
        var r = MathF.Max(0f, radius);
        (int ix0, int iz0) = ToCell(new Vector3(p.X - r, 0, p.Z - r));
        (int ix1, int iz1) = ToCell(new Vector3(p.X + r, 0, p.Z + r));
        for (int iz = Clamp0(iz0, _nz - 1); iz <= Clamp0(iz1, _nz - 1); iz++)
            for (int ix = Clamp0(ix0, _nx - 1); ix <= Clamp0(ix1, _nx - 1); ix++)
                foreach (var t in _buckets[iz * _nx + ix])
                    hs.Add(t);
        return hs;
    }

    public IEnumerable<int> QueryCandidatesAlongSegment(Vector3 a, Vector3 b, float radius = 0f)
    {
        var hs = new HashSet<int>();
        // 선분 AABB + 반경(캡슐)으로 패딩
        float minx = MathF.Min(a.X, b.X) - radius, maxx = MathF.Max(a.X, b.X) + radius;
        float minz = MathF.Min(a.Z, b.Z) - radius, maxz = MathF.Max(a.Z, b.Z) + radius;

        (int ix0, int iz0) = ToCell(new Vector3(minx, 0, minz));
        (int ix1, int iz1) = ToCell(new Vector3(maxx, 0, maxz));
        for (int iz = Clamp0(iz0, _nz - 1); iz <= Clamp0(iz1, _nz - 1); iz++)
            for (int ix = Clamp0(ix0, _nx - 1); ix <= Clamp0(ix1, _nx - 1); ix++)
                foreach (var t in _buckets[iz * _nx + ix])
                    hs.Add(t);
        return hs;
    }

    private (int ix, int iz) ToCell(Vector3 p)
    {
        int ix = (int)MathF.Floor((p.X - _min.X) / _cell + EPS);
        int iz = (int)MathF.Floor((p.Z - _min.Z) / _cell + EPS);
        return (ix, iz);
    }
    private static int Clamp0(int v, int hi) => v < 0 ? 0 : (v > hi ? hi : v);
}
