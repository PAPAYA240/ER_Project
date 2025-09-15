using Assets.Scripts.Highlight;
using Data;
using Google.Protobuf.Protocol;
using System;
using UnityEngine;
using UnityEngine.AI;

public class MonsterController : CreatureController
{
    // 패킷
    private int _lastReceivedSequenceId = -1;

    // 몬스터 정보
    public MonsterSkill Skill { get;  set; }
    public MonsterType _monsterType;
    public float _rotationSpeed = 10f;

    private System.Random _random = new System.Random();
    Quaternion _nextRotation;
    public Vector3 TargetPosition { get; private set; }
    // 애니메이션 끝났을 때 호출
    public Action<CreatureState> OnStateChanged; 

    // TODO : 임시 변수, 나중에 블랙 보드 만들면 없앨 부분
    public bool isSpawned = false;

    // Material 
    private Renderer monsterRenderer;
    private Material originalMaterial;
    private Material skillMaterial;

    protected override void Init()
	{
        ObjectType = Define.Object.Monster; 
		base.Init();
        if (!Add_Component())
        {
            Debug.LogError("MonsterController Add_Component : 컴포넌트 추가 실패");
            return;
        }

        this.gameObject.layer = LayerMask.NameToLayer("Monster");
    }

    protected override void UpdateController()
    {
       transform.rotation = Quaternion.Slerp(transform.rotation, _nextRotation, Time.deltaTime * _rotationSpeed);
       transform.rotation = transform.rotation;

        if(Skill == MonsterSkill.MsSkill2 && State == CreatureState.Skill)
            monsterRenderer.material = skillMaterial;
        else
            monsterRenderer.material = originalMaterial;
    }

    public override void OnDamaged()
	{
		Managers.Object.Remove(Id);
		Managers.Resource.Destroy(gameObject);
	}

    #region 패킷
    public void OnIdlePacket(S_State movePacket)
    {
        _navMeshAgent.SetDestination(transform.position);

        Skill = MonsterSkill.MsNone;

        OnStateChanged?.Invoke(State);
    }

    public void OnMovePacket(S_State packet)
    {
        if (_navMeshAgent == null)
            return;

        _navMeshAgent.SetDestination(new Vector3(packet.PosInfo.PosX, packet.PosInfo.PosY, packet.PosInfo.PosZ));
        _nextRotation = new Quaternion(packet.RotInfo.Qx, packet.RotInfo.Qy, packet.RotInfo.Qz, packet.RotInfo.Qw);
    }

    public void OnSkillPacket(S_State packet)
    {
        _navMeshAgent.ResetPath();
        Skill = packet.Skilltype;

        _navMeshAgent.SetDestination(new Vector3(packet.PosInfo.PosX, packet.PosInfo.PosY, packet.PosInfo.PosZ));
        _nextRotation = new Quaternion(packet.RotInfo.Qx, packet.RotInfo.Qy, packet.RotInfo.Qz, packet.RotInfo.Qw);
    }

    public void OnRecvStatePacket(S_State packet)
    {
        if (packet.SequenceId <= _lastReceivedSequenceId)
        {
            Debug.Log($"오래된 패킷{packet.SequenceId} 무시");
            return;
        }
        _lastReceivedSequenceId = packet.SequenceId;

        State = packet.MyState;
        TargetPosition = new Vector3(packet.TargetPosition.PosX, packet.TargetPosition.PosY, packet.TargetPosition.PosZ);

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
                break;
        }
    }
    #endregion
  

    #region 컴포넌트 추가
    private bool Add_Component()
    {
        _navMeshAgent = GetComponentInParent<NavMeshAgent>();
        if (_navMeshAgent == null)
            return false;
        _navMeshAgent.updateRotation = false;

        monsterRenderer = this.GetComponentInChildren<Renderer>();
        if (monsterRenderer == null)
            return false;
        
        originalMaterial = monsterRenderer.material;
        skillMaterial = Resources.Load<Material>("materials/effect/auraMaterial");
        if (skillMaterial == null)
            return false;
        this.gameObject.AddComponent<HighlightEffect>();

        if (_animator == null)
            return false;
        _animator.applyRootMotion = false;

        return true;
    }
    #endregion
}

