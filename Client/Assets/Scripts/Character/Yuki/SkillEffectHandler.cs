using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using UnityEngine;

public static class SkillEffectHandler
{
    static Dictionary<SkillEffectType, Action<GameObject>> _effectMap
        = new Dictionary<SkillEffectType, Action<GameObject>>();

    static SkillEffectHandler()
    {
        // 원하는 스킬 이펙트 등록
        //Register(Yuki_SkillEffectType.Q, PlayYukiQ);
        Register(SkillEffectType.YukiW, PlayYukiW);
        //Register(Yuki_SkillEffectType.E, PlayYukiE);
        Register(SkillEffectType.YukiR, PlayYukiR);
        Register(SkillEffectType.YukiRShadow, PlayYukiRShadow);
        Register(SkillEffectType.YukiRHit, PlayYukiRHit);
        Register(SkillEffectType.YukiRAttack, PlayYukiRAttack);

        //Register(Yuki_SkillEffectType.Dash, PlayDashEffect);
    }

    public static void Register(SkillEffectType type, Action<GameObject> effect)
    {
        if (!_effectMap.ContainsKey(type))
            _effectMap.Add(type, effect);
    }

    public static void HandleEffect(SkillEffectType type, GameObject owner)
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

    private static void PlayYukiRShadow(GameObject owner)
    {
        owner.GetComponentInChildren<Yuki_SkillShadow>(true)?.PlayEffect();
    }

    private static void PlayYukiRHit(GameObject owner)
    {
        owner.GetComponentInChildren<Yuki_SkillHit>(true)?.PlayEffect();
    }

    private static void PlayYukiRAttack(GameObject owner)
    {
        owner.GetComponentInChildren<Yuki_SkillAttack>(true)?.PlayEffect();
    }

    private static void PlayYukiW(GameObject owner)
    {
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
