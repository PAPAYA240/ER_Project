using System.Collections;
using System.Collections.Generic;
using Google.Protobuf.Protocol;
using UnityEngine;
using static Define;

public class MonsterController : CreatureController
{
	Coroutine _coSkill;

	protected override void Init()
	{
		base.Init();

        _object = Define.Object.Monster;
    }

    protected override void UpdateIdle()
    {
    }

    public override void OnDamaged()
	{
		//Managers.Object.Remove(Id);
		//Managers.Resource.Destroy(gameObject);
	}
    public override void UseSkill(int skillId)
    {
        if (skillId == 1)
        {
			State = CreatureState.Skill;
        }
    }
}
