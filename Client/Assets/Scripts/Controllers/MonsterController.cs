using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.AI;
using static Define;

public class MonsterController : CreatureController
{
    private System.Random _random = new System.Random();
    private S_Move _pendingMovePacket = null;
    public Action<CreatureState> OnStateChanged; // State 변경 시에 호출

    //public enum MonsterSkill
    //{
    //    None = 0,
    //    Attack1 = 1,
    //    Attack2 = 2,
    //    Skill1 = 3,
    //    Skill2 = 4,
    //    Skill3 = 5
    //}

    public MonsterSkill Skill { get;  set; } 

    protected override void Init()
	{
        Skill = MonsterSkill.MsAttack1;
        ObjectType = Define.Object.Monster;
        _navMeshAgent = GetComponentInParent<NavMeshAgent>();
		base.Init();
    }

    protected override void UpdateController()
    {
        base.UpdateController();
    }

    protected override void UpdateMoving()
    {
    }

    public override void OnDamaged()
	{
		//Managers.Object.Remove(Id);
		//Managers.Resource.Destroy(gameObject);
	}

    Vector3 _lastPos;
    Vector3 _currentPos;
    // 서버에서 패킷을 받을 때 호출되는 함수
    public void OnMovePacket(S_State movePacket)
    {
        if (_navMeshAgent == null)
            return;

        _currentPos = new Vector3(movePacket.PosInfo.PosX, movePacket.PosInfo.PosY, movePacket.PosInfo.PosZ);
        _navMeshAgent.SetDestination(new Vector3(movePacket.PosInfo.PosX, movePacket.PosInfo.PosY, movePacket.PosInfo.PosZ));
    }
    public void OnSkillPacket(S_State packet)
    {
        Skill = packet.Skilltype;
        Debug.Log($"몬스터 ID {Skill}: 상태를 SKILL로 변경했습니다.");
        OnStateChanged?.Invoke(State);
    }

    public void OnRecvStatePacket(S_State packet)
    {
        State = packet.MyState;
        switch (State)
        {
            case CreatureState.Idle:
                _navMeshAgent.SetDestination(transform.position);
                break;
            case CreatureState.Moving:
                OnMovePacket(packet);
            break;
            case CreatureState.Skill:
                OnSkillPacket(packet);
                break;
            case CreatureState.Dead:
                _navMeshAgent.SetDestination(transform.position);
             break;
        }
    }

    public void OnSkillAnimationEnd()
    {
    }
    public void SendSkillEndPacket(MonsterSkill _type)
    {
        C_SkillEnd skillPacket = new C_SkillEnd()
        {
            ObjectInfo = ObjInfo,
            SkillType = _type
        };
        Managers.Network.Send(skillPacket);
    }
}