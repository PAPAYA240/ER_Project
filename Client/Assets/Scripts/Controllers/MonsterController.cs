using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.AI;

public class MonsterController : CreatureController
{
    private System.Random _random = new System.Random();
    private S_Move _pendingMovePacket = null;

    public enum MonsterSkill
    {
        None = 0,
        Attack1 = 1,
        Attack2 = 2,
        Skill1 = 3,
        Skill2 = 4,
        Skill3 = 5
    }

    public MonsterSkill Skill { get;  set; } 

    protected override void Init()
	{
        Skill = MonsterSkill.Attack1;
        ObjectType = Define.Object.Monster;
        _navMeshAgent = GetComponentInParent<NavMeshAgent>();
		base.Init();
    }

    protected override void UpdateController()
    {
        base.UpdateController();
    }

    Vector3 _lastPos;
    Vector3 _currentPos;
    // 서버에서 패킷을 받을 때 호출되는 함수
    public void OnRecvMovePacket(S_Move movePacket)
    {
        if (State == CreatureState.Skill || State == CreatureState.Idle)
        {
            _pendingMovePacket = movePacket;
            return;
        }

        _lastPos = transform.position;
        _currentPos = new Vector3(movePacket.PosInfo.PosX, movePacket.PosInfo.PosY, movePacket.PosInfo.PosZ);
        _navMeshAgent.SetDestination(new Vector3(movePacket.PosInfo.PosX, movePacket.PosInfo.PosY, movePacket.PosInfo.PosZ));
    }

    protected override void UpdateMoving()
    {
    }

    public override void OnDamaged()
	{
		//Managers.Object.Remove(Id);
		//Managers.Resource.Destroy(gameObject);
	}

    public override void UseSkill(int skillId)
    {
        Skill = (MonsterSkill)skillId;
        State = CreatureState.Skill;
    }

    public bool isAnimEnd = false;

    public void OnSkillAnimationEnd()
    {
        isAnimEnd = true;
    }
}