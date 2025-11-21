using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public static class AvatarCreatorFromHierarchy
{
    [MenuItem("Tools/Create Generic Avatar From Selected")]
    public static void CreateAvatarFromSelected()
    {
        var go = Selection.activeGameObject;
        if (go == null)
        {
            Debug.LogError("Avatar를 만들 캐릭터 루트를 Hierarchy에서 선택하세요.");
            return;
        }

        // 여기서 "Bip001" 대신 니 캐릭터의 루트 본 이름으로 맞춰줘야 함
        const string rootBoneName = "Root";

        var rootBone = FindChildRecursive(go.transform, rootBoneName);
        if (rootBone == null)
        {
            Debug.LogError($"루트 본 '{rootBoneName}' 을(를) 찾을 수 없습니다.");
            return;
        }

        // Generic Avatar 생성
        var avatar = AvatarBuilder.BuildGenericAvatar(go, AnimationUtility.CalculateTransformPath(rootBone, go.transform));
        if (!avatar.isValid || !avatar.isHuman)
        {
            Debug.LogWarning("Generic Avatar가 생성됐지만 Humanoid는 아니에요. 그래도 Generic + LayerMask에는 사용 가능.");
        }

        string path = $"Assets/{go.name}_Avatar.asset";
        AssetDatabase.CreateAsset(avatar, path);
        AssetDatabase.SaveAssets();

        // Animator에 붙여주기
        var animator = go.GetComponent<Animator>();
        if (animator != null)
        {
            animator.avatar = avatar;
            Debug.Log($"Avatar 생성 및 적용 완료: {path}");
        }
        else
        {
            Debug.LogWarning("선택한 오브젝트에 Animator가 없습니다. Avatar만 생성했습니다.");
        }
    }

    private static Transform FindChildRecursive(Transform root, string name)
    {
        if (root.name == name)
            return root;

        foreach (Transform child in root)
        {
            var found = FindChildRecursive(child, name);
            if (found != null)
                return found;
        }
        return null;
    }
}
