using Newtonsoft.Json;
using Server.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace Server.Game
{
    [System.Serializable]
    public class SerializableVector3
    {
        public float x, y, z;
    }

    // 이것은 내비 메시를 로드하기 위한 것
    [Serializable]
    public class NavMeshExportData
    {
        public List<Vector3> vertices { get; set; }
        public List<int> triangles { get; set; }
        public static NavMeshExportData LoadFromJson(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Error: NavMesh data file not found at {filePath}");
                return null;
            }
            string jsonString = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<NavMeshExportData>(jsonString);
        }
    }
 }