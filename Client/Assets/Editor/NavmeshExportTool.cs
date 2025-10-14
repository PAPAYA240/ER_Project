using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class NavmeshExportTool : EditorWindow
{
    // 설정값
    private string _version = System.DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
    private float _scale = 1f;
    private string _savePath;

    [MenuItem("Tools/NavMeshDataDTO/Export NavMesh JSON…")]
    public static void Open()
    {
        var win = GetWindow<NavmeshExportTool>("NavMesh Exporter");
        win.minSize = new Vector2(420, 160);
        win.InitDefaultPath();
        win.Show();
    }

    private void InitDefaultPath()
    {
        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
        var streamingAssets = Path.Combine(Application.dataPath, "StreamingAssets");
        if (!Directory.Exists(streamingAssets))
            Directory.CreateDirectory(streamingAssets);

        _savePath = Path.Combine(streamingAssets, $"navmesh_{_version}.json");
    }

    private void OnGUI()
    {
        GUILayout.Space(8);
        EditorGUILayout.LabelField("Export Settings", EditorStyles.boldLabel);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            _version = EditorGUILayout.TextField(new GUIContent("Version", "파일 버전/태그 문자열"), _version);
            _scale = EditorGUILayout.FloatField(new GUIContent("Scale", "월드 좌표 스케일(보통 1.0)"), _scale);

            EditorGUILayout.Space(6);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Save Path", GUILayout.Width(70));
            EditorGUILayout.SelectableLabel(_savePath, EditorStyles.textField, GUILayout.Height(18));
            if (GUILayout.Button("Browse", GUILayout.Width(80)))
            {
                var dir = string.IsNullOrEmpty(_savePath) ? Application.dataPath : Path.GetDirectoryName(_savePath);
                var file = Path.GetFileName(string.IsNullOrEmpty(_savePath) ? $"navmesh_{_version}.json" : _savePath);
                var selected = EditorUtility.SaveFilePanel("Export NavMesh JSON", dir, file, "json");
                if (!string.IsNullOrEmpty(selected))
                    _savePath = selected;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_savePath)))
            {
                if (GUILayout.Button("Export NavMesh JSON", GUILayout.Height(32)))
                    DoExport();
            }
        }

        GUILayout.FlexibleSpace();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Open Containing Folder"))
            {
                if (!string.IsNullOrEmpty(_savePath))
                    EditorUtility.RevealInFinder(_savePath);
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", GUILayout.Width(80)))
                Close();
        }
        GUILayout.Space(6);
    }

    private void DoExport()
    {
        try
        {
            // NavmeshExporter.Export(...) 는 앞서 추가한 클래스(런타임/에디터 어디든 OK)를 사용
            var dto = NavmeshExporter.Export(_version, _scale);

            // JsonUtility 는 유니티 기본 직렬화(빠르고 간단)
            var json = JsonUtility.ToJson(dto, prettyPrint: true);

            var dir = Path.GetDirectoryName(_savePath);
            if (string.IsNullOrEmpty(dir) == false && Directory.Exists(dir) == false)
                Directory.CreateDirectory(dir);

            File.WriteAllText(_savePath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            Debug.Log($"[NavMesh Export] Saved: {_savePath}");
            EditorUtility.DisplayDialog("NavMesh Export", "Export completed.\n\n" + _savePath, "OK");
        }
        catch (System.SystemException e)
        {
            Debug.LogError($"[NavMesh Export] Failed: {e}");
            EditorUtility.DisplayDialog("NavMesh Export", "Export failed.\n\n" + e.Message, "OK");
        }
    }
}
#endif
