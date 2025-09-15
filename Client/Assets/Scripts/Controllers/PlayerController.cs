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
    }

    protected override void UpdateController()
    {
        base.UpdateController();
    }

    protected virtual void CheckUpdatedFlag() {}

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
            SkillBase skill = FindSkill((KeyCode)skillPacket.SkillInfo.KeyCode);
            skill.Execute();

            if (Define.Object.MyPlayer == ObjectType)
            {
                Managers.Object.MyPlayer.StartCoCoolTime((KeyCode)skillPacket.SkillInfo.KeyCode, skill.CurLevelCooldown);
            }

            //StartCoroutine(CoStartSkill());
            Debug.Log("스킬 코루틴 시작");
        }
    }

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

    public void PlayAnimFromServer(AnimInfo animInfo)
    {
        _animator.CrossFadeInFixedTime(animInfo.Name, animInfo.Ratio);
    }
  
    #endregion
}
