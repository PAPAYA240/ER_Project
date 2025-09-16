using UnityEditor;
using UnityEngine;

public class RemoveMissingScriptsFromPrefabs
{
    [MenuItem("Tools/Remove Missing Scripts From Selected Prefabs")]
    private static void RemoveMissingScripts()
    {
        Object[] selectedObjects = Selection.objects;

        int totalRemoved = 0;
        int totalPrefabs = 0;

        foreach (Object obj in selectedObjects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path)) continue;

            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefabAsset == null) continue; // Prefab이 아닌 경우 스킵

            // 임시 인스턴스 생성
            GameObject tempPrefab = PrefabUtility.InstantiatePrefab(prefabAsset) as GameObject;
            if (tempPrefab == null) continue;

            // 재귀적으로 하위 계층까지 Missing Script 제거
            RemoveMissingRecursive(tempPrefab, ref totalRemoved, ref totalPrefabs);

            // 원본 Prefab에 변경사항 적용
            PrefabUtility.SaveAsPrefabAsset(tempPrefab, path);
            Object.DestroyImmediate(tempPrefab);
        }

        Debug.Log($"Removed {totalRemoved} missing components from {totalPrefabs} prefabs.");
    }

    private static void RemoveMissing(GameObject go, ref int removed, ref int count)
    {
        int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
        if (missing > 0)
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            removed += missing;
            count++;
        }
    }

    private static void RemoveMissingRecursive(GameObject go, ref int removed, ref int count)
    {
        RemoveMissing(go, ref removed, ref count);
        foreach (Transform child in go.transform)
        {
            RemoveMissingRecursive(child.gameObject, ref removed, ref count);
        }
    }
}