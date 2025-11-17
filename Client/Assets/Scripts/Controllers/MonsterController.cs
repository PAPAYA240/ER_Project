using Google.Protobuf.Protocol;
using System;
using UnityEngine;
using UnityEngine.AI;

public class MonsterController : CreatureController
{
    private System.Random _random = new System.Random();
    
    // 몬스터 정보
    public MonsterSkill Skill { get;  set; }
    public MonsterType Type { get; set; }

 
    public Vector3 TargetPosition { get; private set; }

    private Quaternion _targetRotation;
    private float _rotationSpeed = 10f;
    private float _agentSpeed = 6;

    // 애니메이션 끝났을 때 호출
    public Action<CreatureState, bool> OnStateChanged; 

    // Material 
    private Renderer monsterRenderer;
    private Material originalMaterial;
    private Material skillMaterial;

    // HpBar
    protected GameObject _hpBar;
    private bool _bMesh = false;

    protected override void Init()
	{
        base.Init();

        int monsterLayer = LayerMask.NameToLayer("Monster");
        SetLayerRecursively(this.gameObject, monsterLayer);

        if (!Add_Component())
            return;

        State = CreatureState.Appear;
        InitHpBar();
        UnActiveShaderXRay();
    }

    protected override void UpdateController()
    {
        transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, Time.deltaTime * _rotationSpeed);
        
        MeshDebug();
    }
    private void MeshDebug()
    {
        if (!_bMesh && State == CreatureState.Skill)
        {
            _bMesh = true;
            GameObject skillMeshGO = Managers.Resource.Instantiate("Debug/SkillMesh", this.transform);
            SkillMesh sm = skillMeshGO.GetComponent<SkillMesh>();
            if (sm == null) return;

            if (!DataManager.MonstSkillHitboxDict.ContainsKey(Type))
                return;
            if (!DataManager.MonstSkillHitboxDict[Type].ContainsKey(Skill))
                return;

            SkillHitbox hitbox = DataManager.MonstSkillHitboxDict[Type][Skill];
            sm.Init(hitbox, this.transform, 0, 0, this.GetMouseWorldPosition());
        }
    }
    public Vector3 GetMouseWorldPosition()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) return Vector3.zero;

        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Map")))
            return hit.point;

        return Vector3.zero;
    }
    public override void OnDamaged()
    {
    }

    public override void OnDead()
    {
        if (State != CreatureState.Dead)
            State = CreatureState.Dead;

        if(_hpBar)
            _hpBar.SetActive(false);

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

        if (packet.RotInfo != null)
            _targetRotation = new Quaternion(packet.RotInfo.Qx, packet.RotInfo.Qy, packet.RotInfo.Qz, packet.RotInfo.Qw);

        Skill = MonsterSkill.MsNone;

        OnStateChanged?.Invoke(State, true);
    }

    public void OnMovePacket(S_State packet)
    {
        if (_agent != null)
             _agent.SetDestination(packet.PosInfo.ToVector());

        if(packet.RotInfo != null)
            _targetRotation = new Quaternion(packet.RotInfo.Qx, packet.RotInfo.Qy, packet.RotInfo.Qz, packet.RotInfo.Qw);
    }

    public void OnSkillPacket(S_State packet)
    {
        Skill = packet.Skilltype;

        if (_agent != null)
        {
            _agent.ResetPath();
           _agent.SetDestination(packet.PosInfo.ToVector());
        }

        if(packet.RotInfo != null)
            _targetRotation = new Quaternion(packet.RotInfo.Qx, packet.RotInfo.Qy, packet.RotInfo.Qz, packet.RotInfo.Qw);
        OnStateChanged?.Invoke(State, false);
    }

    public void OnRecvStatePacket(S_State packet)
    {
        State = packet.MyState;
        if (packet.TargetPosition != null)
            TargetPosition = packet.TargetPosition.ToVector();

        if (State == CreatureState.Skill)
            _bMesh = false;

        if(Type == MonsterType.Turret)
            Debug.Log($"Turret STATE : {State}");

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
            case CreatureState.Appear:
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

    private HighlightEffect _highlightEffect;
    private bool Add_Component()
    {
        _agent = GetComponentInParent<NavMeshAgent>();
        if (_agent != null)
        {
            _agent.updatePosition = true;
            _agent.updateRotation = true;
            _agent.speed = _agentSpeed;
            SyncPos(true);
        }

        monsterRenderer = this.GetComponentInChildren<Renderer>();
        if (monsterRenderer == null)
            return false;

        _targetRotation = transform.rotation;
        originalMaterial = monsterRenderer.material;
        skillMaterial = Resources.Load<Material>("materials/effect/auraMaterial");
        _highlightEffect = gameObject.AddComponent<HighlightEffect>();
        _highlightEffect.Owner = this;

        if (_animator == null)
            return false;
        _animator.applyRootMotion = false;

        return true;
    }
    #endregion

    #region 체력바
    private void InitHpBar()
    {
        switch (Type)
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

        if(Type != MonsterType.Drone)
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

    #region 유틸
    public Vector3 GetTargetForwardVector()
    {
        return _targetRotation * Vector3.forward;
    }
    #endregion
}

