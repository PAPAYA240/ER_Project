#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

[System.Serializable]
public class SpawnPointExportDTO
{
    public int id;
    public string side;   // "Blue", "Red"
    public string type;   // "BaseSpawn", "BushTeleport"
    public float x;
    public float y;
    public float z;
}

[System.Serializable]
public class MapSpawnExportRoot
{
    public List<SpawnPointExportDTO> spawns = new();
}

public static class SpawnPointExporter
{
    [MenuItem("Tools/Export SpawnPoints To JSON")]
    public static void Export()
    {
        // 현재 씬에 있는 SpawnPointMarker 전부 찾기
        SpawnPointMarker[] markers = Object.FindObjectsByType<SpawnPointMarker>(FindObjectsSortMode.None);
        if (markers.Length == 0)
        {
            Debug.LogWarning("SpawnPointMarker 가 씬에 하나도 없습니다.");
            return;
        }

        var root = new MapSpawnExportRoot();

        foreach (var m in markers)
        {
            Vector3 pos = m.GetPosition(); // 아까 만든 GetPosition()

            var dto = new SpawnPointExportDTO
            {
                id = m.id,
                side = m.side == true ? "1" : "0",
                type = m.type.ToString(),   // "BaseSpawn", "BushTeleport"
                x = pos.x,
                y = pos.y,
                z = pos.z
            };

            root.spawns.Add(dto);
        }

        string json = JsonUtility.ToJson(root, true);

        // 저장 경로 선택
        string path = EditorUtility.SaveFilePanel(
            "Export SpawnPoints",
            Application.dataPath,
            "SpawnPoints.json",
            "json");

        if (string.IsNullOrEmpty(path))
            return;

        File.WriteAllText(path, json);
        Debug.Log($"SpawnPoints exported: {path}");
    }
}
#endif