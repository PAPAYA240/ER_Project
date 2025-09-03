using Google.Protobuf.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
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
		MakeSkillDict();
        _object = Define.Object.OtherPlayer; 
    }

	protected void MakeSkillDict()
	{
        var skillTypes = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsSubclassOf(typeof(SkillBase)) && !t.IsAbstract);

        foreach (var type in skillTypes)
        {
            // TODO : 캐릭터 별로 다르게 넣어주기
            // SkillBase를 상속받은 클래스들을 탐색해 생성
            // 클래스의 이름으로 SkillDict에서 SkillData을 검색해 데이터를 채워줌
            SkillBase skill = (SkillBase)Activator.CreateInstance(type);
            skill.SkillData = Managers.Data.SkillDict[type.Name];
            skill._player = this;
            skill._animator = this._animator;
            _skills.Add(type.Name, skill);
        }
    }

    protected SkillBase FindSkill(string skillName)
    {
        SkillBase skillBase = null;
        if (!_skills.TryGetValue(skillName, out skillBase))
        {
            Debug.Log($"Skill을 찾을 수 없음 : {skillName}");
            return null;
        }

        return skillBase;
    }

    protected override void UpdateAnimation()
	{
		if (_animator == null)
			return;

		if (State == CreatureState.Idle)
		{
			
		}
		else if (State == CreatureState.Moving)
		{
			
		}
		else if (State == CreatureState.Skill)
		{

		}
		else
		{

		}
	}

	protected override void UpdateController()
	{		
		base.UpdateController();
	}

    public override void UseSkill(string skillName)
    {
        SkillBase skill = FindSkill(skillName);
        skill.Execute();

        if(_coSkill != null)
            StopCoroutine(_coSkill);
        _coSkill = StartCoroutine("CoStartSkill");
        Debug.Log("스킬 코루틴 시작");
    }

    public virtual void PlayAnimation(AnimInfo animInfo)
    {
        if(!animInfo.IsTrigger)
            _animator.Play(animInfo.Hash, 0, Time.time - animInfo.Time);
        else
            _animator.SetTrigger(animInfo.Hash);     
    }

    protected virtual void CheckUpdatedFlag()
	{

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

        _animator.SetTrigger("tIdle");
        CheckUpdatedFlag();
    }

    public override void OnDamaged()
	{
		Debug.Log("Player HIT !");
	}
}
