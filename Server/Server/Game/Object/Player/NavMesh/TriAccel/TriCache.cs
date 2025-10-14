using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

public sealed class TriCache
{
    public struct Tri
    {
        public int i0, i1, i2;
        public Vector3 v0, v1, v2;
        public Vector3 normal;
        public Vector3 aabbMin, aabbMax;
        public float d; // plane: normal·x + d = 0
    }

    public Tri[] Tris;
}

public static class TriCacheBuilder
{
    public static TriCache Build(NavmeshData nav)
    {
        var tris = new TriCache.Tri[nav.Indices.Length / 3];
        for (int t = 0; t < tris.Length; t++)
        {
            int i0 = nav.Indices[t * 3 + 0];
            int i1 = nav.Indices[t * 3 + 1];
            int i2 = nav.Indices[t * 3 + 2];

            var v0 = nav.Verts[i0];
            var v1 = nav.Verts[i1];
            var v2 = nav.Verts[i2];

            var n = Vector3.Normalize(Vector3.Cross(v1 - v0, v2 - v0));
            var d = -Vector3.Dot(n, v0);

            var aabbMin = Vector3.Min(v0, Vector3.Min(v1, v2));
            var aabbMax = Vector3.Max(v0, Vector3.Max(v1, v2));

            tris[t] = new TriCache.Tri
            {
                i0 = i0,
                i1 = i1,
                i2 = i2,
                v0 = v0,
                v1 = v1,
                v2 = v2,
                normal = n,
                d = d,
                aabbMin = aabbMin,
                aabbMax = aabbMax
            };
        }
        return new TriCache { Tris = tris };
    }
}