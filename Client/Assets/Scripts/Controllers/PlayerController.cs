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
        ObjectType = Define.Object.OtherPlayer; 
    }

	protected override void UpdateController()
	{		
		base.UpdateController();
	}

    protected virtual void CheckUpdatedFlag()
	{

	}

    public override void OnDamaged()
	{
		Debug.Log("Player HIT !");
	}

    #region Skill
    public override void UseSkill(KeyCode key)
    {
        SkillBase skill = FindSkill(key);
        skill.Execute();

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

        _animator.SetTrigger("tIdle");
        CheckUpdatedFlag();
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

    protected SkillBase FindSkill(KeyCode key)
    {
        SkillBase skillBase = null;

        string skillName = Enum.GetName(typeof(Character), Managers.Object.Character) + '_' + key.ToString();
        if (!_skills.TryGetValue(skillName, out skillBase))
        {
            Debug.Log($"Skill을 찾을 수 없음 : {key}");
            return null;
        }

        return skillBase;
    }
    #endregion

    #region Animation
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

    // 서버로부터 애니메이션 정보를 받아와 다른 플레이어의 애니메이션 재생용
    public virtual void PlayAnimation(AnimInfo animInfo)
    {
        switch (animInfo.Type)
        {
            case AnimType.Play:
                _animator.Play(animInfo.Hash, 0, Time.time - animInfo.Value);
                break;
            case AnimType.Trigger:
                _animator.SetTrigger(animInfo.Hash);
                break;
            case AnimType.Bool:
                _animator.SetBool(animInfo.Hash, animInfo.Value != 0f);
                break;
            case AnimType.Float:
                _animator.SetFloat(animInfo.Hash, animInfo.Value);
                break;
        }
    }
    #endregion
}
