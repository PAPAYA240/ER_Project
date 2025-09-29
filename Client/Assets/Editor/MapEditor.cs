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
        public List<SerializableVector3> vertices;
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
        NavMeshTriangulation navMeshTriangulation = NavMesh.CalculateTriangulation();
        if (navMeshTriangulation.vertices == null || navMeshTriangulation.vertices.Length == 0)
            return;

        NavMeshExportData exportData = new NavMeshExportData();

        exportData.vertices = new List<SerializableVector3>();
        foreach (Vector3 v in navMeshTriangulation.vertices)
            exportData.vertices.Add(new SerializableVector3 { x = v.x, y = v.y, z = v.z });

        exportData.triangles = new List<int>(navMeshTriangulation.indices);
        string json = Newtonsoft.Json.JsonConvert.SerializeObject(exportData, Newtonsoft.Json.Formatting.Indented);
        string path = Path.Combine(Application.dataPath, "Resources/Data/MonsterData", exportFileName);
        File.WriteAllText(path, json);

        Debug.Log($"NavMesh 데이터 저장 성공! : {path}");
        AssetDatabase.Refresh(); 
    }
}

// 네비 메시 디버거
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

