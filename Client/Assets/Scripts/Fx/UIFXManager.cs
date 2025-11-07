using Google.Protobuf.Protocol;
using System.Collections.Generic;
using UnityEngine;

public class UIFXManager : MonoBehaviour
{
    private readonly Dictionary<CharacterType, string> _statusMarkEffectPaths = new Dictionary<CharacterType, string>
    {
        [CharacterType.Theodore] = "Prefabs/UI/Character/Theodore/FX_BI_Theodore_Skill03_Target_Mark"
    };

    private Dictionary<int, List<GameObject>> currentlyPlayingMarks = new Dictionary<int, List<GameObject>>();
    public void Init()
    {
        foreach (var pair in _statusMarkEffectPaths)
        {
            GameObject prefab = Resources.Load<GameObject>(pair.Value);
            if (prefab != null)
                Managers.FX.CreatePool(prefab, 10);
        }
    }

    public void PlayStatusEffect(GameObject target, CharacterType charType, float duration)
    {
        if (target == null || !_statusMarkEffectPaths.ContainsKey(charType)) 
            return;

        string prefabPath = _statusMarkEffectPaths[charType];
        GameObject prefab = Managers.Resource.Load<GameObject>(prefabPath);
        if (prefab == null) 
            return;

        StopStatusEffect(target, charType);

        GameObject fxObject = Managers.FX.Pop(prefab, target.transform);
        if (fxObject == null)
        { 
            Managers.FX.CreatePool(prefab, 1);
            fxObject = Managers.FX.Pop(prefab, target.transform);
            if (fxObject == null)
            {
                Debug.LogError($"UIFXManager: Failed to pop poolable for {prefabPath}");
                return;
            }
        }

        UI_TargetingMark mark = fxObject.GetOrAddComponent<UI_TargetingMark>();
        if (!currentlyPlayingMarks.ContainsKey(target.GetInstanceID()))
        {
            currentlyPlayingMarks[target.GetInstanceID()] = new List<GameObject>();
        }
        currentlyPlayingMarks[target.GetInstanceID()].Add(fxObject);

        mark.Show(target, duration, () => {
            if (currentlyPlayingMarks.ContainsKey(target.GetInstanceID()))
            {
                currentlyPlayingMarks[target.GetInstanceID()].Remove(fxObject);
                Managers.FX.Push(fxObject);
            }
        });
    }

    public void StopStatusEffect(GameObject target, CharacterType effectName)
    {
        if (currentlyPlayingMarks.TryGetValue(target.GetInstanceID(), out List<GameObject> effectList))
        {
            GameObject markToStop = null;
            string targetName = effectName.ToString();

            foreach (GameObject mark in effectList)
            {
                if (mark.name.Replace("(Clone)", "") == targetName)
                {
                    markToStop = mark;
                    break;
                }
            }

            if (markToStop != null)
            {
                markToStop.GetComponent<UI_TargetingMark>()?.Hide();

                currentlyPlayingMarks[target.GetInstanceID()].Remove(markToStop);
                Managers.FX.Push(markToStop);
            }
        }
    }

    // 특정 타겟의 모든 마크를 강제로 중지 및 정리
    public void RemoveAllMarks(int ownerId)
    {
        if (currentlyPlayingMarks.TryGetValue(ownerId, out List<GameObject> effectList))
        {
            foreach (GameObject mark in new List<GameObject>(effectList))
            {
                mark.GetComponent<UI_TargetingMark>()?.Hide();
                Managers.FX.Push(mark);
            }
            effectList.Clear();
            currentlyPlayingMarks.Remove(ownerId);
        }
    }

    public void Clear()
    {
        currentlyPlayingMarks.Clear();
    }
}