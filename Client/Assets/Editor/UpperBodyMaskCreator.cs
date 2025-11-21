using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public static class UpperBodyMaskCreator
{
    [MenuItem("Tools/Create Upper Body Mask From Selected")]
    public static void CreateUpperBodyMaskFromSelected()
    {
        var root = Selection.activeGameObject;
        if (root == null)
        {
            Debug.LogError("상체 마스크를 만들 캐릭터 루트를 Hierarchy에서 선택하고 다시 실행하세요.");
            return;
        }

        // 새 마스크 생성
        var mask = new AvatarMask();

        // 선택한 오브젝트 기준으로 전체 뼈 트리 추가
        AddTransformsRecursive(mask, root.transform);

        // 이름 기준으로 상체/하체 구분
        for (int i = 0; i < mask.transformCount; i++)
        {
            string path = mask.GetTransformPath(i);

            bool isUpper =
                path.Contains("Spine") ||
                path.Contains("Chest") ||
                path.Contains("Neck") ||
                path.Contains("Head") ||
                path.Contains("Clavicle") ||
                path.Contains("Shoulder") ||
                path.Contains("UpperArm") ||
                path.Contains("Arm") ||
                path.Contains("Forearm") ||
                //path.Contains("ForeTwist") ||
                path.Contains("Hand");
                path.Contains("Finger");

            // 다리 쪽은 이름에 Thigh/Calf/Foot 이런 거 들어있는 경우가 많음
            bool isLeg =
                path.Contains("Thigh") ||
                path.Contains("Calf") ||
                path.Contains("Foot") ||
                path.Contains("Toe");

            // 상체는 켜고, 다리나 기타는 끔
            bool active = isUpper && !isLeg;
            mask.SetTransformActive(i, active);
        }

        // 에셋 저장
        string assetPath = "Assets/UpperBodyMask.asset";
        AssetDatabase.CreateAsset(mask, assetPath);
        AssetDatabase.SaveAssets();

        Debug.Log($"UpperBodyMask 생성 완료: {assetPath}");
    }

    private static void AddTransformsRecursive(AvatarMask mask, Transform t)
    {
        mask.AddTransformPath(t);
        foreach (Transform child in t)
            AddTransformsRecursive(mask, child);
    }
}

