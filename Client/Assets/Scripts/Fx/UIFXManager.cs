using Google.Protobuf.Protocol;
using System.Collections.Generic;
using UnityEngine;

public class UIFXManager : MonoBehaviour
{
    private readonly Dictionary<CharacterType, string> _statusMarkEffectPaths = new Dictionary<CharacterType, string>
    {
        [CharacterType.Theodore] = "Prefabs/UI/Character/Theodore/FX_BI_Theodore_Skill03_Target_Mark"
    };

    private Dictionary<int, Dictionary<CharacterType, GameObject>> currentlyPlayingMarks =
        new Dictionary<int, Dictionary<CharacterType, GameObject>>();

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

        // 기존 같은 타입 이펙트 중지
        StopStatusEffect(target, charType);

        GameObject fxObject = Managers.FX.Pop(prefab, null);

        if (fxObject == null)
        {
            Managers.FX.CreatePool(prefab, 1);
            fxObject = Managers.FX.Pop(prefab, null);
            if (fxObject == null)
                return;
        }

        fxObject.transform.position = target.transform.position;
        fxObject.transform.rotation = Quaternion.identity;
        fxObject.transform.localScale = Vector3.one;

        UI_TargetingMark mark = fxObject.GetComponentInChildren<UI_TargetingMark>();

        if (mark == null)
        {
            Managers.FX.Push(fxObject);
            return;
        }

        int targetId = target.GetInstanceID();

        // Dictionary 구조 초기화
        if (!currentlyPlayingMarks.ContainsKey(targetId))
        {
            currentlyPlayingMarks[targetId] = new Dictionary<CharacterType, GameObject>();
        }

        currentlyPlayingMarks[targetId][charType] = fxObject;

        mark.Show(target, duration, () => {
            // 콜백에서 풀로 반환
            if (currentlyPlayingMarks.ContainsKey(targetId) &&
                currentlyPlayingMarks[targetId].ContainsKey(charType))
            {
                currentlyPlayingMarks[targetId].Remove(charType);
                if (currentlyPlayingMarks[targetId].Count == 0)
                    currentlyPlayingMarks.Remove(targetId);
            }
            Managers.FX.Push(fxObject);
        });
    }

    public void StopStatusEffect(GameObject target, CharacterType charType)
    {
        int targetId = target.GetInstanceID();

        if (currentlyPlayingMarks.TryGetValue(targetId, out var effectDict))
        {
            if (effectDict.TryGetValue(charType, out GameObject markObject))
            {
                UI_TargetingMark mark = markObject.GetComponentInChildren<UI_TargetingMark>();
                mark?.Hide();

                effectDict.Remove(charType);
                if (effectDict.Count == 0)
                    currentlyPlayingMarks.Remove(targetId);

                Managers.FX.Push(markObject);
            }
        }
    }

    public void RemoveAllMarks(int targetId)
    {
        if (currentlyPlayingMarks.TryGetValue(targetId, out var effectDict))
        {
            foreach (var pair in new Dictionary<CharacterType, GameObject>(effectDict))
            {
                UI_TargetingMark mark = pair.Value.GetComponentInChildren<UI_TargetingMark>();
                mark?.Hide();
                Managers.FX.Push(pair.Value);
            }
            effectDict.Clear();
            currentlyPlayingMarks.Remove(targetId);
        }
    }

    public void Clear()
    {
        foreach (var targetDict in currentlyPlayingMarks.Values)
        {
            foreach (var markObject in targetDict.Values)
            {
                UI_TargetingMark mark = markObject.GetComponentInChildren<UI_TargetingMark>();
                mark?.Hide();
                Managers.FX.Push(markObject);
            }
        }
        currentlyPlayingMarks.Clear();
    }
}