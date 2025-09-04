﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.IO;
using UnityEngine.AI;
using Newtonsoft.Json; // Added


#if UNITY_EDITOR
using UnityEditor;
#endif

public class NavMeshExporter : EditorWindow
{
    [System.Serializable]
    public class SerializableVector3
    {
        public float x, y, z;
    }

    [System.Serializable]
    public class NavMeshExportData
    {
        public List<SerializableVector3> vertices; // 이 유형을 변경합니다.
        public List<int> triangles;
    }

    private string exportFileName = "navmesh_data.json";

    [MenuItem("Tools/Export NavMesh Data")]
    public static void ShowWindow()
    {
        GetWindow<NavMeshExporter>("NavMesh Exporter");
    }

    void OnGUI()
    {
        GUILayout.Label("NavMesh Export Settings", EditorStyles.boldLabel);
        exportFileName = EditorGUILayout.TextField("Export File Name", exportFileName);

        if (GUILayout.Button("Export NavMesh"))
        {
            ExportNavMesh();
        }
    }

    void ExportNavMesh()
    {
        // 현재 NavMesh 삼각 측량 데이터 가져오기
        NavMeshTriangulation navMeshTriangulation = NavMesh.CalculateTriangulation();

        if (navMeshTriangulation.vertices == null || navMeshTriangulation.vertices.Length == 0)
        {
            Debug.LogError("NavMesh 데이터없다. 먼저 NavMesh를 베이킹하샘!");
            return;
        }

        // 내보내기를 위한 직렬화 가능한 데이터 구조 생성
        NavMeshExportData exportData = new NavMeshExportData();

        exportData.vertices = new List<SerializableVector3>();
        foreach (Vector3 v in navMeshTriangulation.vertices)
        {
            exportData.vertices.Add(new SerializableVector3 { x = v.x, y = v.y, z = v.z });
        }

        exportData.triangles = new List<int>(navMeshTriangulation.indices);
        string json = Newtonsoft.Json.JsonConvert.SerializeObject(exportData, Newtonsoft.Json.Formatting.Indented);
        string path = Path.Combine(Application.dataPath, "Resources", exportFileName);
        File.WriteAllText(path, json);

        Debug.Log($"NavMesh 데이터가 다음으로 내보내졌습니다: {path}");
        AssetDatabase.Refresh(); 
    }
}
public class NavMeshDebug : EditorWindow
{
    private string navMeshFilePath = "Assets/Resources/navmesh_data.json"; // NavMesh JSON 파일 경로
    private int triangleIndexToDebug = -1; // 디버그할 삼각형 인덱스
    private NavMeshExportData navMeshData;

    [System.Serializable]
    public class SerializableVector3
    {
        public float x, y, z;
    }

    [System.Serializable]
    public class NavMeshExportData
    {
        public List<SerializableVector3> vertices;
        public List<int> triangles;
    }

    [MenuItem("Tools/NavMesh Debugger")]
    public static void ShowWindow()
    {
        GetWindow<NavMeshDebug>("NavMesh Debugger");
    }

    private void OnGUI()
    {
        GUILayout.Label("NavMesh Debugger", EditorStyles.boldLabel);
        navMeshFilePath = EditorGUILayout.TextField("NavMesh JSON Path", navMeshFilePath);
        triangleIndexToDebug = EditorGUILayout.IntField("Triangle Index", triangleIndexToDebug);

        if (GUILayout.Button("Load NavMesh Data"))
        {
            LoadNavMeshData();
        }
    }

    private void LoadNavMeshData()
    {
        if (File.Exists(navMeshFilePath))
        {
            string json = File.ReadAllText(navMeshFilePath);
            navMeshData = JsonUtility.FromJson<NavMeshExportData>(json);
            Debug.Log("NavMesh 데이터 로드 완료.");
        }
        else
        {
            Debug.LogError("파일을 찾을 수 없습니다: " + navMeshFilePath);
        }
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (navMeshData != null && navMeshData.triangles.Count > 0 && triangleIndexToDebug >= 0 && navMeshData.triangles.Count / 3 > triangleIndexToDebug)
        {
            // 삼각형의 정점 인덱스 가져오기
            int v0_index = navMeshData.triangles[triangleIndexToDebug * 3];
            int v1_index = navMeshData.triangles[triangleIndexToDebug * 3 + 1];
            int v2_index = navMeshData.triangles[triangleIndexToDebug * 3 + 2];

            // 정점 위치 가져오기
            Vector3 v0 = new Vector3(navMeshData.vertices[v0_index].x, navMeshData.vertices[v0_index].y, navMeshData.vertices[v0_index].z);
            Vector3 v1 = new Vector3(navMeshData.vertices[v1_index].x, navMeshData.vertices[v1_index].y, navMeshData.vertices[v1_index].z);
            Vector3 v2 = new Vector3(navMeshData.vertices[v2_index].x, navMeshData.vertices[v2_index].y, navMeshData.vertices[v2_index].z);

            // 삼각형의 중심 계산
            Vector3 center = (v0 + v1 + v2) / 3.0f;

            // 시각화
            Handles.color = Color.red;
            Handles.SphereHandleCap(0, center, Quaternion.identity, 0.5f, Event.current.type);
            Handles.Label(center, triangleIndexToDebug.ToString());

            // 와이어프레임 삼각형 그리기
            Handles.DrawPolyLine(v0, v1, v2, v0);
        }
    }
}
#if UNITY_EDITOR

// % (Ctrl), # (Shift), & (Alt)

//[MenuItem("Tools/GenerateMap %#g")]
//private static void GenerateMap()
//{
//	GenerateByPath("Assets/Resources/Map");
//       GenerateByPath("../Common/MapData");
//}

//private static void GenerateByPath(string pathPrefix)
//{
//       GameObject[] gameObjects = Resources.LoadAll<GameObject>("Prefabs/Map");

//       foreach (GameObject go in gameObjects)
//       {
//           Tilemap tmBase = Util.FindChild<Tilemap>(go, "Tilemap_Base", true);
//           Tilemap tm = Util.FindChild<Tilemap>(go, "Tilemap_Collision", true);

//           using (var writer = File.CreateText($"{pathPrefix}/{go.name}.txt"))
//           {
//               writer.WriteLine(tmBase.cellBounds.xMin);
//               writer.WriteLine(tmBase.cellBounds.xMax);
//               writer.WriteLine(tmBase.cellBounds.yMin);
//               writer.WriteLine(tmBase.cellBounds.yMax);

//               for (int y = tmBase.cellBounds.yMax; y >= tmBase.cellBounds.yMin; y--)
//               {
//                   for (int x = tmBase.cellBounds.xMin; x <= tmBase.cellBounds.xMax; x++)
//                   {
//                       TileBase tile = tm.GetTile(new Vector3Int(x, y, 0));
//                       if (tile != null)
//                           writer.Write("1");
//                       else
//                           writer.Write("0");
//                   }
//                   writer.WriteLine();
//               }
//           }
//       }
//   }

#endif

