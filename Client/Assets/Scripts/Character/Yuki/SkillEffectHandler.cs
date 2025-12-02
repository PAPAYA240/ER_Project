using Google.Protobuf.Protocol;
using System.Collections.Generic;
using UnityEngine;

public interface IEffect
{
    void Play();
    void Stop();
}

public class SkillEffectHandler
{
    private Dictionary<SkillEffectType, IEffect> _effectMap = new Dictionary<SkillEffectType, IEffect>();

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
            (_effectMap[SkillEffectType.QBuff] as MonoBehaviour).transform.localPosition = Vector3.zero;
            (_effectMap[SkillEffectType.RAttack] as MonoBehaviour).transform.localPosition = new Vector3(0, 1f, 0);
            (_effectMap[SkillEffectType.QAttack] as MonoBehaviour).transform.localPosition = new Vector3(0, 1f, 1f);
        }
    }

    private void RegisterEffect(SkillEffectType type, PlayerController player, string prefabPath)
    {
        GameObject prefab = Managers.Resource.Instantiate(prefabPath); 
        prefab.transform.SetParent(player.transform);
        prefab.transform.localPosition = Vector3.zero;

        // Fx_YukiEffect, Fx_YukiFlower, Fx_YukiR �� � Ÿ���̾ �ڵ����� ã��
        IEffect effectComp = prefab.GetComponentInChildren<IEffect>();

        if (effectComp == null) 
            effectComp = prefab.AddComponent<Fx_YukiEffect>();

        _effectMap[type] = effectComp;
    }

    private void RegisterEffectBone(SkillEffectType type, PlayerController player, string prefabPath, string boneName)
    {
        GameObject prefab = Managers.Resource.Instantiate(prefabPath);

        Transform bone = Util.FindChildByName(player.transform, boneName).transform;
        prefab.transform.SetParent(bone);


        // Fx_YukiEffect, Fx_YukiFlower, Fx_YukiR �� � Ÿ���̾ �ڵ����� ã��
        IEffect effectComp = prefab.GetComponentInChildren<IEffect>();

        if (effectComp == null)
            effectComp = prefab.AddComponent<Fx_YukiEffect>();

        _effectMap[type] = effectComp;
    }

    public void PlayEffect(SkillEffectType type)
    {
        if (_effectMap.TryGetValue(type, out var effect))
        {
            effect.Play();
        }
    }

    public void StopEffect(SkillEffectType type)
    {
        if (_effectMap.TryGetValue(type, out var effect))
        {
            effect.Stop();
        }
    }
}
