using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.AI;

[System.Serializable]
public class NavmeshDataDTO
{
    public string version;
    public float[] verts;     // xyz 평탄화
    public int[] indices;     // 삼각형 인덱스
    public int[] areas;       // (옵션)
    public float[] bounds;    // AABB
    public float[] up;        // [0,1,0]
    public float scale = 1f;
}

public static class NavmeshExporter
{
    public static NavmeshDataDTO Export(string version = null, float scale = 1f)
    {
        var tri = NavMesh.CalculateTriangulation();
        var vs = tri.vertices.SelectMany(v => new[] { v.x * scale, v.y * scale, v.z * scale }).ToArray();
        var idx = tri.indices.ToArray();
        var ars = tri.areas?.ToArray() ?? new int[idx.Length / 3];

        var (min, max) = CalcBounds(tri.vertices, scale);
        return new NavmeshDataDTO
        {
            version = version ?? System.DateTime.UtcNow.ToString("yyyyMMdd_HHmmss"),
            verts = vs,
            indices = idx,
            areas = ars,
            bounds = new[] { min.X, min.Y, min.Z, max.X, max.Y, max.Z },
            up = new[] { 0f, 1f, 0f },
            scale = scale
        };
    }

    static (Vector3 min, Vector3 max) CalcBounds(UnityEngine.Vector3[] arr, float s)
    {
        Vector3[] v = ToNumericsArray(arr);

        var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        foreach (var p in v)
        { var q = p * s; min = Vector3.Min(min, q); max = Vector3.Max(max, q); }
        return (min, max);
    }

    public static System.Numerics.Vector3[] ToNumericsArray(this UnityEngine.Vector3[] arr)
    {
        var n = arr.Length;
        var r = new System.Numerics.Vector3[n];
        for (int i = 0; i < n; i++)
            r[i] = new System.Numerics.Vector3(arr[i].x, arr[i].y, arr[i].z);
        return r;
    }
}
