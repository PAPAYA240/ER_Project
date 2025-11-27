using Google.Protobuf.Protocol;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

public class SpawnPointExportDTO
{
    public int id { get; set; }
    public string side { get; set; }   // "0", "1"
    public string type { get; set; }   // "BaseSpawn", "BushTeleport", "BushNone"
    public float x { get; set; }
    public float y { get; set; }
    public float z { get; set; }
}

public class MapSpawnExportRoot
{
    public List<SpawnPointExportDTO> spawns { get; set; } = new();
}

public static class SpawnPointLoader
{
    public static void LoadSpawnPoints(string jsonPath, SpawnPointRegistry registry)
    {
        if (!File.Exists(jsonPath))
            throw new FileNotFoundException($"SpawnPoint json not found: {jsonPath}");

        string json = File.ReadAllText(jsonPath);
        var root = JsonConvert.DeserializeObject<MapSpawnExportRoot>(json);

        foreach (var dto in root.spawns)
        {
            var info = new SpawnPointInfo
            {
                Id = dto.id,
                Team = dto.side == "1" ? true : false,              
                Type = Enum.Parse<SpawnPointType>(dto.type),        // "BaseSpawn" -> SpawnPointType.BaseSpawn
                Position = new Vector3(dto.x, dto.y, dto.z)
            };

            registry.Add(info);
        }
    }
}