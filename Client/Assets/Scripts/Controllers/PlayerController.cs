using Data;
using Google.Protobuf.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Windows;
using static Define;
using static System.Runtime.CompilerServices.RuntimeHelpers;

public class PlayerController : CreatureController
{
    bool _isKeyInput = false;
    int _atkCount = 1;
    int _maxAtkCount = 2;

    //Fog
    private FogOfWarVision _fogOfWarVision;

    //NameTag
    protected GameObject _nameTag; 

    // 레이어
    protected string layerName;

    public bool IsKeyInput
    {
        get { return _isKeyInput; }
        set
        {
            _isKeyInput = value;
            Debug.Log($"IsKeyInput changed: {value}");
        }
    }

    public int AttackCount
    {
        get { return _atkCount; }
        set { _atkCount = value; }
    }

    public int MaxAttackCount
    {
        get { return _maxAtkCount; }
        set { _maxAtkCount = value; }
    }

    protected override void Init()
    {
        base.Init();

        ObjectType = Define.Object.OtherPlayer;

        //Fog
        _fogOfWarVision = gameObject.GetOrAddComponent<FogOfWarVision>();
        gameObject.layer = LayerMask.NameToLayer("Fog");

        InitNameTag();
    }

    protected override void UpdateController()
    {
        base.UpdateController();
    }

    protected virtual void CheckUpdatedFlag() { }

    public override void OnDamaged()
    {
        Debug.Log("Player HIT !");
    }

    #region Util
    protected string GetCharacterName()
    {
        return Enum.GetName(typeof(CharacterType), ObjInfo.CharType);
    }
    #endregion

    #region Skill
    public override void UseSkill(S_Skill skillPacket)
    {
        Debug.Log("스킬 패킷 받기");

        // 서버에서 스킬 사용을 허락받으면
        if (skillPacket.CanUse)
        {
            KeyCode keyCode = (KeyCode)skillPacket.SkillInfo.KeyCode;
            ExecuteSkill(keyCode);

            if (Define.Object.MyPlayer == ObjectType)
            {
                Managers.Object.MyPlayer.StartCoCoolTime(keyCode);
            }

            State = CreatureState.Skill;
            //StartCoroutine(CoStartSkill());
            Debug.Log("스킬 코루틴 시작");

            CreateSkillMesh((KeyCode)skillPacket.SkillInfo.KeyCode);
        }
    }

    protected void ExecuteSkill(KeyCode keyCode)
    {
        switch (keyCode)
        {
            case KeyCode.Q:
                Skill_Q();
                break;
            case KeyCode.W:
                Skill_W();
                break;
            case KeyCode.E:
                Skill_E();
                break;
            case KeyCode.R:
                Skill_R();
                break;
            case KeyCode.F:
                PassiveSkill();
                break;
        }
    }

    // TODO : 이름 바꾸기?
    protected virtual void Skill_Q() { }

    protected virtual void Skill_W() { }

    protected virtual void Skill_E() { }

    protected virtual void Skill_R() { }
    protected virtual void PassiveSkill() { }

    IEnumerator CoStartSkill()
    {
        // 대기 시간
        IsKeyInput = true;
        State = CreatureState.Skill;
        yield return new WaitForSeconds(0.1f);
        AnimatorClipInfo[] clipInfos = _animator.GetCurrentAnimatorClipInfo(0);
        float length = 0;
        if (clipInfos.Length > 0)
        {
            length = clipInfos[0].clip.length / _animator.speed;
            Debug.Log($"Clip Name: {clipInfos[0].clip.name}, Length: {length}");
        }
        yield return new WaitForSeconds(length - 0.1f);
        Debug.Log("스킬 코루틴 종료");

        // TODO : TEMP
        CheckUpdatedFlag();
    }
    #endregion

    #region Animation
    protected virtual void PlayAnimation(string animName, float ratio)
    {
        int layerIndex = _animator.GetLayerIndex(layerName);
        if (layerIndex == -1)
            return;

        _animator.CrossFadeInFixedTime(animName, ratio);
    }
    public virtual void OnAttackTiming() { }

    public void PlayAnimFromServer(AnimInfo animInfo)
    {
        _animator.CrossFadeInFixedTime(animInfo.Name, animInfo.Ratio);
    }

    #endregion

    public void PlayEffectFromServer(EffectInfo fxInfo)
    {
        Managers.FX.PlayEffect(Find_EffectList((KeyCode)fxInfo.KeyCode), this.transform);
    }

    #region SkillMesh

    void CreateSkillMesh(KeyCode keyCode)
    {
        SkillHitbox skillHitbox = DataManager.SkillHitboxDict[ObjInfo.CharType][keyCode];
        GameObject go = Managers.Resource.Instantiate("Debug/SkillMesh", gameObject.transform);
        SkillMesh sm = go.GetComponent<SkillMesh>();
        if (sm == null) return;
        sm.Init(skillHitbox, gameObject.transform, ObjInfo.Team);
    }

    #endregion

    #region NameTag
    protected void InitNameTag()
    {
        _nameTag = Managers.Resource.Instantiate("UI/SubItem/PlayerNameTagCanvas", gameObject.transform);
        if (null == _nameTag)
        {
            Debug.Log("_nameTag is null");
            return;
        }

        _nameTag.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        UI_PlayerNameTag ui = _nameTag.GetComponentInChildren<UI_PlayerNameTag>();
        ui.SetTarget(gameObject);
        ui.SetHPColor();
    }
    #endregion
    protected List<EffectData> Find_EffectList(KeyCode key)
    {
        var skillDict = DataManager.PlayerFxDict[ObjInfo.CharType];
        if (skillDict.ContainsKey(key))
            return skillDict[key];
        return null;
    }
}
