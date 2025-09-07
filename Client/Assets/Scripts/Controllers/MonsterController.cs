using Google.Protobuf.Protocol;
using System;
using UnityEngine;
using UnityEngine.AI;

public class MonsterController : CreatureController
{
    private System.Random _random = new System.Random();
    private S_Move _pendingMovePacket = null;
    public Action<CreatureState> OnStateChanged; // State 변경 시에 호출
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

    public void OnIdlePacket(S_State movePacket)
    {
        _navMeshAgent.SetDestination(transform.position);
       // _navMeshAgent.isStopped = true;
        OnStateChanged?.Invoke(State);
    }

    Vector3 _currentPos;
    // 서버에서 패킷을 받을 때 호출되는 함수
    public void OnMovePacket(S_State movePacket)
    {
        if (_navMeshAgent == null)
            return;
       // _navMeshAgent.isStopped = false;

        _currentPos = new Vector3(movePacket.PosInfo.PosX, movePacket.PosInfo.PosY, movePacket.PosInfo.PosZ);
        _navMeshAgent.SetDestination(new Vector3(movePacket.PosInfo.PosX, movePacket.PosInfo.PosY, movePacket.PosInfo.PosZ));
    }

    public void OnSkillPacket(S_State packet)
    {
        _navMeshAgent.SetDestination(transform.position);
        //_navMeshAgent.isStopped = true;
        Skill = packet.Skilltype;
        _navMeshAgent.SetDestination(new Vector3(packet.PosInfo.PosX, packet.PosInfo.PosY, packet.PosInfo.PosZ));
    }

    public void OnRecvStatePacket(S_State packet)
    {
        State = packet.MyState;
        if (_navMeshAgent == null)
            return;
        switch (State)
        {
            case CreatureState.Idle:
                OnIdlePacket(packet);
                break;
            case CreatureState.Moving:
                OnMovePacket(packet);
            break;
            case CreatureState.Skill:
                OnSkillPacket(packet);
                break;
            case CreatureState.Dead:
                
                //_navMeshAgent.SetDestination(transform.position);
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