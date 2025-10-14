using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Newtonsoft.Json;

public sealed class NavmeshData
{
    public Vector3[] Verts;             // world space
    public int[] Indices;               // 3개씩 삼각형
    public int[] Areas;                 // optional
    public Vector3 Min, Max;
    public string Version;
}

public class NavmeshDataDTO
{
    public string version;
    public float[] verts;
    public int[] indices;
    public int[] areas;
    public float[] bounds;
    public float[] up;
    public float scale = 1f;
}

public static class NavmeshImporter
{
    public static NavmeshData LoadFromJson(string path)
    {
        var json = System.IO.File.ReadAllText(path);
        var dto = JsonConvert.DeserializeObject<NavmeshDataDTO>(json);

        var verts = new Vector3[dto.verts.Length / 3];
        for (int i = 0; i < verts.Length; i++)
        {
            verts[i] = new Vector3(
                dto.verts[i * 3 + 0], dto.verts[i * 3 + 1], dto.verts[i * 3 + 2]);
        }
        var min = new Vector3(dto.bounds[0], dto.bounds[1], dto.bounds[2]);
        var max = new Vector3(dto.bounds[3], dto.bounds[4], dto.bounds[5]);

        return new NavmeshData
        {
            Verts = verts,
            Indices = dto.indices,
            Areas = dto.areas ?? new int[dto.indices.Length / 3],
            Min = min,
            Max = max,
            Version = dto.version
        };
    }
}