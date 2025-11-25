using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using UnityEngine;

public static class Yuki_SkillEffectHandler
{
    static Dictionary<Yuki_SkillEffectType, Action<GameObject>> _effectMap
        = new Dictionary<Yuki_SkillEffectType, Action<GameObject>>();

    static Yuki_SkillEffectHandler()
    {
        // 원하는 스킬 이펙트 등록
        //Register(Yuki_SkillEffectType.Q, PlayYukiQ);
        Register(Yuki_SkillEffectType.W, PlayYukiW);
        //Register(Yuki_SkillEffectType.E, PlayYukiE);
        Register(Yuki_SkillEffectType.R, PlayYukiR);

        //Register(Yuki_SkillEffectType.Dash, PlayDashEffect);
    }

    public static void Register(Yuki_SkillEffectType type, Action<GameObject> effect)
    {
        if (!_effectMap.ContainsKey(type))
            _effectMap.Add(type, effect);
    }

    public static void HandleEffect(Yuki_SkillEffectType type, GameObject owner)
    {
        if (_effectMap.TryGetValue(type, out var effectAction))
            effectAction(owner);
        else
            Debug.LogWarning($"Effect not implemented: {type}");
    }

    private static void PlayYukiR(GameObject owner)
    {
        owner.GetComponentInChildren<YukiSkillRange>(true)?.PlayEffectOneSecond();
    }

    private static void PlayYukiW(GameObject owner)
    {
        //// 예시
        owner.GetComponentInChildren<YukiFlower>(true)?.ActivateYukiPyosik();
    }

    private static void PlayExplosion(GameObject owner)
    {
        // 파티클 instantiate 등
    }

    private static void PlayDash(GameObject owner)
    {
        // 잔상, 트레일, 파티클 등
    }
}
