using Data;
using Google.Protobuf.Protocol;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerEffectController : MonoBehaviour
{
    private PlayerController _player;

    public void Init(PlayerController player)
    {
        _player = player;
    }

    // 서버에서 받은 이펙트 재생 요청 처리
    public void PlayEffect(S_Fx packet, Vector3 mousePos, Vector3 targetPos = new Vector3(), Quaternion targetRot = default(Quaternion))
    {
        Transform targetTransform = null;
        if (packet.UseTargetTransform)
        {
            GameObject go = Managers.Object.FindById(packet.TargetId);
            if (go == null)
                return;
            targetTransform = go.transform;
        }

        if (!packet.IsCommon)
        {
            if (packet.Type == "Caster")
                PlayEffect((KeyCode)packet.SkillKey, mousePos, targetPos, targetRot, targetTransform: targetTransform);
            else if (packet.Type == "Select")
                PlayEffect((KeyCode)packet.SkillKey, mousePos, targetPos, targetRot, packet.FxName, targetTransform: targetTransform);
        }
        else
        {
            if (packet.Type == "Caster")
                PlayEffect(packet.CommonName, mousePos, targetPos, targetRot);
            else if (packet.Type == "Select")
                PlayEffect(packet.CommonName, packet.FxName, mousePos, targetPos, targetRot);
        }
    }

    // 기본 이펙트 : Type Caster (자동 호출)
    public void PlayEffect(KeyCode skillKey, Vector3 mousePos, Vector3 targetPos, Quaternion targetRot = default(Quaternion), Transform targetTransform = null)
    {
        CharacterType type = _player.ObjInfo.Player.CharType;
        CreatureState state = CreatureState.Skill;

        if (!DataManager.PlayerFxDict.ContainsKey(type))
            return;
        if (!DataManager.PlayerFxDict[type].ContainsKey(state))
            return;
        if (!DataManager.PlayerFxDict[type][state].ContainsKey(skillKey))
            return;

        SkillEffectList myEffectList = DataManager.PlayerFxDict[type][state][skillKey];
        List<EffectData> dataList = new List<EffectData>();
        foreach (EffectData effect in myEffectList.Caster)
        {
            dataList.Add(effect);
        }

        Managers.FX.PlayEffect(_player.ObjInfo.ObjectId, dataList, targetTransform ? targetTransform : transform, mousePos);
    }

    // 기본 이펙트 : Type Select (선택 호출)
    public void PlayEffect(KeyCode skillKey, Vector3 mousePos, Vector3 targetPos, Quaternion targetRot, string fxName, Transform targetTransform = null)
    {
        CharacterType type = _player.ObjInfo.Player.CharType;
        CreatureState state = CreatureState.Skill;

        if (!DataManager.PlayerFxDict.ContainsKey(type))
            return;
        if (!DataManager.PlayerFxDict[type].ContainsKey(state))
            return;
        if (!DataManager.PlayerFxDict[type][state].ContainsKey(skillKey))
            return;

        SkillEffectList myEffectList = DataManager.PlayerFxDict[type][state][skillKey];
        if (myEffectList?.Select == null)
            return;

        List<EffectData> dataList = myEffectList.Select
       .Where(effect => effect != null && effect.prefabName == fxName)
       .ToList();

        if (dataList.Count == 0)
            return;

        Managers.FX.PlayEffect(_player.ObjInfo.ObjectId, dataList, targetTransform ? targetTransform : transform, mousePos, targetPos, targetRot);
    }

    // 공통 이펙트 : Type Common (자동 호출)
    public void PlayEffect(string commonName, Vector3 mousePos, Vector3 targetPos, Quaternion targetRot, Transform targetTransform = null)
    {
        if (DataManager.CommonFxDict == null)
            return;

        if (!DataManager.CommonFxDict.TryGetValue(commonName, out SkillEffectList effectList))
            return;

        var dataList = new List<EffectData>();
        if (effectList.Caster != null)
            dataList.AddRange(effectList.Caster);

        Managers.FX.PlayEffect(_player.ObjInfo.ObjectId, dataList, targetTransform ? targetTransform : transform, mousePos, targetPos, targetRot, isCommon: true);
    }

    // 공통 이펙트 : Type Common (선택 호출)
    public void PlayEffect(string commonName, string fxName, Vector3 mousePos, Vector3 targetPos, Quaternion targetRot, Transform targetTransform = null)
    {
        if (DataManager.CommonFxDict == null)
            return;

        if (!DataManager.CommonFxDict.TryGetValue(commonName, out SkillEffectList effectList))
            return;

        var dataList = new List<EffectData>();

        if (effectList.Select != null)
        {
            foreach (EffectData effect in effectList.Select)
            {
                if (effect.prefabName == fxName)
                    dataList.Add(effect);
            }
        }

        if (dataList.Count == 0)
            return;

        Managers.FX.PlayEffect(_player.ObjInfo.ObjectId, dataList, targetTransform ? targetTransform : transform, mousePos, targetPos, targetRot, isCommon: true);
    }
}
