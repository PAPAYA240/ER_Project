using Google.Protobuf.Protocol;
using System;
using UnityEngine;
using UnityEngine.AI;
using static UnityEditor.PlayerSettings;

public class MonsterController : CreatureController
{
    private System.Random _random = new System.Random();
    
    // 몬스터 정보
    public MonsterSkill Skill { get;  set; }
    public MonsterType _monsterType;
    public float _rotationSpeed = 10f;

    Quaternion _nextRotation;
    public Vector3 _targetPos { get; private set; }

    // 애니메이션 끝났을 때 호출
    public Action<CreatureState> OnStateChanged; 

    // TODO : 임시 변수, 나중에 블랙 보드 만들면 없앨 부분
    public bool isSpawned = false;

    // Material 
    private Renderer monsterRenderer;
    private Material originalMaterial;
    private Material skillMaterial;

    // HpBar
    protected GameObject _hpBar;

    protected override void Init()
	{
        base.Init();

        ObjectType = Define.Object.Monster;
        int monsterLayer = LayerMask.NameToLayer("Monster");
        SetLayerRecursively(this.gameObject, monsterLayer);

        // init
        if (!Add_Component())
            return;

        InitHpBar();
        
        Stat = Stat;
    }
    protected override void UpdateController()
    {
        transform.rotation = Quaternion.Slerp(transform.rotation, _nextRotation, Time.deltaTime * _rotationSpeed);
        transform.rotation = transform.rotation;
    }
    public override void OnDamaged()
    {
    }

    public override void OnDead()
    {
        if (State != CreatureState.Dead)
            State = CreatureState.Dead;
        Hp = 0;
    }

    #region 패킷
    public void OnDeadPacket(S_State packet)
    {
        _agent.ResetPath();
        OnDead();
    }
    public void OnIdlePacket(S_State packet)
    {
        if (_agent != null)
            _agent.SetDestination(packet.PosInfo.ToVector());

        _nextRotation = new Quaternion(packet.RotInfo.Qx, packet.RotInfo.Qy, packet.RotInfo.Qz, packet.RotInfo.Qw);

        Skill = MonsterSkill.MsNone;

        OnStateChanged?.Invoke(State);
    }

    public void OnMovePacket(S_State packet)
    {
        if (_agent != null)
             _agent.SetDestination(packet.PosInfo.ToVector());

        _nextRotation = new Quaternion(packet.RotInfo.Qx, packet.RotInfo.Qy, packet.RotInfo.Qz, packet.RotInfo.Qw);
    }

    public void OnSkillPacket(S_State packet)
    {
        Skill = packet.Skilltype;

        if (_agent != null)
        {
            _agent.ResetPath();
           _agent.SetDestination(packet.PosInfo.ToVector());
        }
        _nextRotation = new Quaternion(packet.RotInfo.Qx, packet.RotInfo.Qy, packet.RotInfo.Qz, packet.RotInfo.Qw);
    }

    public void OnRecvStatePacket(S_State packet)
    {
        State = packet.MyState;
        if(packet.TargetPosition != null)
            _targetPos = new Vector3(packet.TargetPosition.PosX, packet.TargetPosition.PosY, packet.TargetPosition.PosZ);
        Debug.Log($"{State}");

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
                OnDeadPacket(packet);
                break;
        }
    }
    #endregion

    #region 컴포넌트 추가
    public void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, newLayer);
    }

    private bool Add_Component()
    {
        if(_monsterType == MonsterType.Turret)
            _agent = GetComponentInParent<NavMeshAgent>();
        _agent = GetComponentInParent<NavMeshAgent>();
        if (_agent != null)
        {
            _agent.updatePosition = true;
            _agent.updateRotation = true;
            SyncPos(true);
        }

        monsterRenderer = this.GetComponentInChildren<Renderer>();
        if (monsterRenderer == null)
            return false;
        
        originalMaterial = monsterRenderer.material;
        skillMaterial = Resources.Load<Material>("materials/effect/auraMaterial");
        gameObject.AddComponent<HighlightEffect>();

        if (_animator == null)
            return false;
        _animator.applyRootMotion = false;

        return true;
    }
    #endregion

    #region 체력바
    private void InitHpBar()
    {
        switch (_monsterType)
        {
            case MonsterType.Alpha:
            case MonsterType.Omega:
            case MonsterType.Gamma:
                _hpBar = Managers.Resource.Instantiate("UI/SubItem/MonsterHpBar_Boss", gameObject.transform);
                break;
            case MonsterType.Drone:
                _hpBar = Managers.Resource.Instantiate("UI/SubItem/MonsterHpBar_Common", gameObject.transform);
                break;

        }

        if (_hpBar == null) return;

        _hpBar.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        UI_MonsterHpBar ui = _hpBar.GetComponentInChildren<UI_MonsterHpBar>();

        if (null == ui)
        {
            Debug.Log("_hpBar is null");
            return;
        }

        ui.SetTarget(gameObject);
    }

    protected override void UpdateHp()
    {
        //base.UpdateHp();

        if (_hpBar == null)
            return;

        _hpBar.GetComponentInChildren<UI_BarTick>().SetValue(Hp);

        if(_monsterType != MonsterType.Drone)
            _hpBar.GetComponentInChildren<UI_MonsterHpBar>().SetHpText(Hp.ToString("F0"));
    }

    protected override void UpdateMaxHp()
    {
        //base.UpdateHp();

        if (_hpBar == null)
            return;

        _hpBar.GetComponentInChildren<UI_BarTick>().SetMaxValue(MaxHp);
    }

    #endregion
}

