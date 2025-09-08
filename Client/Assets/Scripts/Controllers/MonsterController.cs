using Google.Protobuf.Protocol;
using System;
using UnityEngine;
using UnityEngine.AI;

public class MonsterController : CreatureController
{
    private int _lastReceivedSequenceId = -1;

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

        _navMeshAgent.updateRotation = false;
        _animator.applyRootMotion = false;
    }

    public float rotationInterpolationSpeed = 10f;
    protected override void UpdateController()
    {
            transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, Time.deltaTime * rotationInterpolationSpeed);
            transform.rotation = transform.rotation;
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
        OnStateChanged?.Invoke(State);
    }

    Vector3 _currentPos;
    Quaternion _targetRotation;
    // 서버에서 패킷을 받을 때 호출되는 함수
    public void OnMovePacket(S_State movePacket)
    {
        if (_navMeshAgent == null)
            return;

        _currentPos = new Vector3(movePacket.PosInfo.PosX, movePacket.PosInfo.PosY, movePacket.PosInfo.PosZ);
        _navMeshAgent.SetDestination(_currentPos);
        _targetRotation = new Quaternion(movePacket.RotInfo.Qx, movePacket.RotInfo.Qy, movePacket.RotInfo.Qz, movePacket.RotInfo.Qw);
    }

    public void OnSkillPacket(S_State packet)
    {
        _navMeshAgent.ResetPath();
        Skill = packet.Skilltype;
    }


    public void OnRecvStatePacket(S_State packet)
    {
        if (packet.SequenceId <= _lastReceivedSequenceId)
        {
            Debug.Log($"오래된 패킷 무시: 현재 시퀀스 ID {_lastReceivedSequenceId}, 받은 시퀀스 ID {packet.SequenceId}");
            return;
        }
        _lastReceivedSequenceId = packet.SequenceId;

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

