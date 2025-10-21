using UnityEngine;
using Data;
using Google.Protobuf.Protocol;
using System.Collections.Generic;


public class FXManager : MonoBehaviour
{
    public EffectFXManager Effect { get; private set; }
    public UIFXManager UI { get; private set; }

    protected PoolManager _pool = new PoolManager();
    public void Init()
    {
        _pool.Init();

        GameObject effectGO = new GameObject("EffectFXManager");
        effectGO.transform.SetParent(this.transform);
        Effect = effectGO.AddComponent<EffectFXManager>();
        Effect.Init(_pool);

        // UIFXManager 생성 및 자식으로 설정
        GameObject uiGO = new GameObject("UIFXManager");
        uiGO.transform.SetParent(this.transform); 
        UI = uiGO.AddComponent<UIFXManager>();
        UI.Init(_pool);
    }

    public List<GameObject> PlayEffect(int ownerId, List<EffectData> effectData, Transform casterTransform, Vector3 targetPos = new Vector3(), Quaternion rot = new Quaternion())
    {
        return Effect.PlayEffect(ownerId, effectData, casterTransform, targetPos, rot);
    }

    public void PlayStatusEffect(GameObject target, CharacterType effectName, float duration)
    {
        UI.PlayStatusEffect(target, effectName, duration);
    }

    // 기타 유틸리티 
    public void RemoveAllEffect(int ownerId)
    {
        Effect.RemoveAllEffect(ownerId);
        UI.RemoveAllMarks(ownerId);
    }

    public void Clear()
    {
        Effect.Clear();
        UI.Clear();
    }
}
