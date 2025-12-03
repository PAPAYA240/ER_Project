using System;
using System.Collections;
using System.Collections.Generic;
using Google.Protobuf.Protocol;
using UnityEngine;

public interface IEffect
{
    void Play();
    void Stop();
}

public class SkillEffectHandler
{
    private Dictionary<string, IEffect> _effectMap = new Dictionary<string, IEffect>();
    private Dictionary<string, Coroutine> _activeCoroutines = new Dictionary<string, Coroutine>();
    private MonoBehaviour _coroutineRunner; // 코루틴을 실행할 MonoBehaviour

    public void InitEffects(PlayerController player)
    {
        // ��� ĳ�� ���� ����Ʈ
        RegisterEffect(SkillEffectType.RHit, player, "Effect/Yuki/Yuki_Pyosik_Hit");

        GameObject yukiPyosik = Managers.Resource.Instantiate("Effect/Yuki/UIpyosik");
        yukiPyosik.transform.SetParent(player.transform);

        // Yuki ���� ����Ʈ
        if (player.ObjInfo.Player.CharType == CharacterType.Yuki)
        {
            RegisterEffect(SkillEffectType.RShadow, player, "Effect/Yuki/Yuki_Skill_Shadow");
            RegisterEffect(SkillEffectType.RAttack, player, "Effect/Yuki/Yuki_SkillR_Attack");
            RegisterEffect(SkillEffectType.QAttack, player, "Effect/Yuki/Yuki_SkillQ_Attack");
            RegisterEffect(SkillEffectType.WEffect, player, "Effect/Yuki/Yuki_SkillW");
            RegisterEffectBone(SkillEffectType.QBuff, player, "Effect/Yuki/Yuki_SkillQ_Buff", "Fx_Hand_R");

            // ���� ���� Ŀ���� Fx Ŭ����
            RegisterEffect(SkillEffectType.WFlower, player, "Effect/Yuki/YukiFlower");
            RegisterEffect(SkillEffectType.RRange, player, "Effect/Yuki/Yuki_R");

            //(_effectMap[SkillEffectType.RShadow] as MonoBehaviour).transform.localPosition = Vector3.zero;
            (_effectMap[GetFxKey(SkillEffectType.QBuff)] as MonoBehaviour).transform.localPosition = Vector3.zero;
            (_effectMap[GetFxKey(SkillEffectType.RAttack)] as MonoBehaviour).transform.localPosition = new Vector3(0, 1f, 0);
            (_effectMap[GetFxKey(SkillEffectType.QAttack)] as MonoBehaviour).transform.localPosition = new Vector3(0, 1f, 1f);
        }
        else if(player.ObjInfo.Player.CharType == CharacterType.Abigail)
        {
            // "Fx_Hand_L", "Fx_Top","Fx_Hand_R" ,"Fx_Axe_bottom", "Fx_Axe_Blade","Fx_Axe_Head", "Fx_Center", "Fx_Bottom"
            RegisterEffectBone(AbigailFx.Attack01, player, "Effect/Abigail/FX_BI_Abigail_NormalAttack_01_Axe", "Fx_Center");
            RegisterEffectBone(AbigailFx.Attack02, player, "Effect/Abigail/FX_BI_Abigail_NormalAttack_02_Axe", "Fx_Center");
            RegisterEffectBone(AbigailFx.RestEnd, player, "Effect/Abigail/FX_BI_Abigail_Rest_End", "Fx_Center");
            RegisterEffectBone(AbigailFx.RestStart, player, "Effect/Abigail/FX_BI_Abigail_Rest_Start", "Fx_Center");
            RegisterEffectBone(AbigailFx.QAttack, player, "Effect/Abigail/FX_BI_Abigail_Skill01_02_Attack", "Fx_Bottom");
            RegisterEffectBone(AbigailFx.QAttack2, player, "Effect/Abigail/FX_BI_Abigail_Skill01_02_Attack_Second", "Fx_Bottom");
            //RegisterEffectBone(AbigailFx.QRange, player, "Effect/Abigail/FX_BI_Abigail_Skill01_02_Range", "Fx_Bottom");
            RegisterEffectBone(AbigailFx.WAttack, player, "Effect/Abigail/FX_BI_Abigail_Skill02_02_Attack", "Fx_Center");
            RegisterEffectBone(AbigailFx.WRange, player, "Effect/Abigail/FX_BI_Abigail_Skill02_02_Range", "Fx_Bottom");
            RegisterEffectBone(AbigailFx.EPortal, player, "Effect/Abigail/FX_BI_Abigail_Skill03_Portal", "Fx_Center");
            RegisterEffectBone(AbigailFx.RRange, player, "Effect/Abigail/FX_BI_Abigail_Skill04_Range", "Fx_Bottom");
            RegisterEffectBone(AbigailFx.RStart, player, "Effect/Abigail/FX_BI_Abigail_Skill04_Start", "Fx_Center");
            RegisterEffectBone(AbigailFx.WpnSkill, player, "Effect/Abigail/FX_BI_Abigail_WSkill_Axe_02", "Fx_Center");

            foreach (var effect in _effectMap.Values)
                (effect as MonoBehaviour).transform.localPosition = Vector3.zero;
        }

        // ��� ����Ʈ �ʱ� ��Ȱ��ȭ
        //foreach (var effect in _effectMap.Values)
        //    (effect as MonoBehaviour).gameObject.SetActive(false);

        _coroutineRunner = player;
    }

    private void RegisterEffect<T>(T type, PlayerController player, string prefabPath) where T : Enum
    {
        GameObject prefab = Managers.Resource.Instantiate(prefabPath); 
        prefab.transform.SetParent(player.transform);
        prefab.transform.localPosition = Vector3.zero;

        // Fx_YukiEffect, Fx_YukiFlower, Fx_YukiR �� � Ÿ���̾ �ڵ����� ã��
        IEffect effectComp = prefab.GetComponentInChildren<IEffect>();

        if (effectComp == null) 
            effectComp = prefab.AddComponent<Fx_YukiEffect>();

        _effectMap[GetFxKey(type)] = effectComp;
    }

    private void RegisterEffectBone<T>(T type, PlayerController player, string prefabPath, string boneName) where T : Enum
    {
        GameObject prefab = Managers.Resource.Instantiate(prefabPath);

        Transform bone = Util.FindChildByName(player.transform, boneName).transform;
        prefab.transform.SetParent(bone);


        // Fx_YukiEffect, Fx_YukiFlower, Fx_YukiR �� � Ÿ���̾ �ڵ����� ã��
        IEffect effectComp = prefab.GetComponentInChildren<IEffect>();

        if (effectComp == null)
            effectComp = prefab.AddComponent<Fx_YukiEffect>();

        _effectMap[GetFxKey(type)] = effectComp;
    }

    public void PlayEffect<T>(T type, float duration = 0f) where T : Enum
    {
        if (float.IsNaN(duration) || float.IsInfinity(duration))
            return;

        duration = Mathf.Max(0f, duration);

        string key = GetFxKey(type);

        if (_effectMap.TryGetValue(key, out var effect))
        {
            // 1. 효과 재생
            effect.Play();

            // 2. 지속시간이 있으면 자동 정지 예약
            if (duration >= 0.01f)
            {
                StopCoroutineAndCleanup(key);

                // 새 코루틴 시작
                var newCoroutine = _coroutineRunner.StartCoroutine(AutoStopCoroutine(key, duration));
                _activeCoroutines[key] = newCoroutine;
            }
        }
    }

    public void StopEffect<T>(T type) where T : Enum
    {
        string key = GetFxKey(type);
        StopCoroutineAndCleanup(key);

        // 효과 강제 정지
        if (_effectMap.TryGetValue(key, out var effect))
        {
            effect.Stop();
        }
    }

    public void StopAllEffects()
    {
        // 현재 실행 중인 모든 자동 정지 코루틴을 정지
        foreach (var entry in _activeCoroutines)
        {
            _coroutineRunner.StopCoroutine(entry.Value);
        }
        _activeCoroutines.Clear(); // 모든 코루틴 기록 삭제

        // _effectMap에 등록된 모든 이펙트 오브젝트들을 강제 정지
        foreach (var entry in _effectMap)
        {
            entry.Value.Stop();
        }
    }

    private void StopCoroutineAndCleanup(string effectKey)
    {
        if (_activeCoroutines.TryGetValue(effectKey, out var oldCoroutine))
        {
            _coroutineRunner.StopCoroutine(oldCoroutine);
            _activeCoroutines.Remove(effectKey);
        }
    }

    private IEnumerator AutoStopCoroutine(string effectKey, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 지속시간 종료 시 효과 정지
        if (_effectMap.TryGetValue(effectKey, out var effect))
        {
            effect.Stop();
        }

        // 코루틴 제거
        _activeCoroutines.Remove(effectKey);
    }

    string GetFxKey<T>(T type) where T : Enum
    {
        return $"{typeof(T).Name}_{type}";
    }
}
