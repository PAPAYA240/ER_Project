using System.Collections;
using System.Collections.Generic;
using Google.Protobuf.Protocol;
using UnityEngine;
using static Define;

public class PlayerController : CreatureController
{
	protected Coroutine _coSkill;
    protected bool _rangedSkill = false;

	protected override void Init()
	{
		base.Init();
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

	public override void UseSkill(int skillId)
	{
		if (skillId == 1)
		{
			_coSkill = StartCoroutine("CoStartPunch");
			Debug.Log("Skill Q !");
		}
		else if (skillId == 2)
		{
            _coSkill = StartCoroutine("CoStartShootArrow");
            Debug.Log("Skill W !");
        }
		else if (skillId == 3)
		{
			_coSkill = StartCoroutine("CoStartSkillTemp");
			Debug.Log("Skill E !");
		}
		else if (skillId == 4)
		{
			_coSkill = StartCoroutine("CoStartSkillTemp2");
			Debug.Log("Skill R !");
		}
	}

	protected virtual void CheckUpdatedFlag()
	{

	}

	IEnumerator CoStartPunch()
	{
		// 대기 시간
		_rangedSkill = false;
		State = CreatureState.Skill;
		yield return new WaitForSeconds(0.5f);
		State = CreatureState.Idle;
		_coSkill = null;
		CheckUpdatedFlag();
    }

	IEnumerator CoStartShootArrow()
	{
		// 대기 시간
		_rangedSkill = true;
        State = CreatureState.Skill;
        yield return new WaitForSeconds(0.3f);
		State = CreatureState.Idle;
		_coSkill = null;
        CheckUpdatedFlag();
    }

    IEnumerator CoStartSkillTemp()
    {
        // 대기 시간
        _rangedSkill = true;
        State = CreatureState.Skill;
        yield return new WaitForSeconds(0.5f);
        State = CreatureState.Idle;
        _coSkill = null;
        CheckUpdatedFlag();
    }

    IEnumerator CoStartSkillTemp2()
    {
        // 대기 시간
        _rangedSkill = true;
        State = CreatureState.Skill;
        yield return new WaitForSeconds(0.1f);
        State = CreatureState.Idle;
        _coSkill = null;
        CheckUpdatedFlag();
    }

    public override void OnDamaged()
	{
		Debug.Log("Player HIT !");
	}
}
