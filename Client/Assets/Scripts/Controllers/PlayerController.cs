using Google.Protobuf.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static Define;

public class PlayerController : CreatureController
{
    protected Coroutine _coSkill;
    protected bool _rangedSkill = false;

    protected Dictionary<string, SkillBase> _skills = new Dictionary<string, SkillBase>();

    protected override void Init()
    {
        base.Init();
        //MakeSkillDict();
        ObjectType = Define.Object.OtherPlayer;
    }

    protected override void UpdateAnimation()
    {
    }

    #region Util
    protected string GetCharacterName()
    {
        return Enum.GetName(typeof(CharacterType), ObjInfo.CharType);
    }
    #endregion

    protected override void UpdateController()
    {
        base.UpdateController();
    }

    public override void UseSkill(string keyCode)
    {
        //SkillBase skill = FindSkill(keyCode);
        //skill.Execute();

        if (_coSkill != null)
            StopCoroutine(_coSkill);
        _coSkill = StartCoroutine("CoStartSkill");
        Debug.Log("스킬 코루틴 시작");
    }

    IEnumerator CoStartSkill()
    {
        // 대기 시간
        _rangedSkill = false;
        State = CreatureState.Skill;
        float length = _animator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(length);
        State = CreatureState.Idle;
        _coSkill = null;

        // TODO : TEMP
        _animator.SetTrigger("tIdle");
        CheckUpdatedFlag();
    }

    protected void MakeSkillDict()
    {
        var skillTypes = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsSubclassOf(typeof(SkillBase)) && !t.IsAbstract);

        foreach (var type in skillTypes)
        {
            // SkillBase를 상속받은 클래스들을 탐색해 생성
            // 클래스의 이름으로 SkillDict에서 SkillData을 검색해 데이터를 채워줌

            // 본인 캐릭터의 스킬 정보만 추출
            string className = type.Name;
            int idx = className.IndexOf('_');
            string charName = idx >= 0 ? className.Substring(0, idx) : className;
            if (charName != GetCharacterName())
                continue;

            SkillBase skill = (SkillBase)Activator.CreateInstance(type);
            skill.SkillData = Managers.Data.SkillDict[type.Name];
            skill._player = this;
            skill._animator = this._animator;
            _skills.Add(type.Name, skill);
        }
    }

    protected virtual void CheckUpdatedFlag()
    {

    }

    public override void OnDamaged()
    {
        Debug.Log("Player HIT !");
    }

    public void PlayAnimFromServer(AnimInfo animInfo)
    {
        _animator.CrossFadeInFixedTime(animInfo.Name, animInfo.Ratio);
    }
}
