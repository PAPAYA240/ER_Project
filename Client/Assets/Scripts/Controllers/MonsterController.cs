using Data;
using Google.Protobuf.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class MonsterController : CreatureController
{

    #region 몬스터 정보
    [SerializeField] private MonsterType type;
    public MonsterSkill Skill { get;  set; }
    public int MonsterTeam { get; set; }

    public MonsterType Type
    {
        get => type;
        set => type = value;
    }
    public override CreatureState State
    {
        get { return PosInfo.State; }
        set
        {
            if (PosInfo.State == value)
                return;

            PosInfo.State = value;
            _updated = true;
        }
    }
    #endregion

    #region Component
    public SoundController Sound;
    private VisualEffectController _highlightEffect;

    protected GameObject _hpBar;
    #endregion

    #region Val
    public Vector3 TargetPosition { get; private set; }
    private Quaternion _targetRotation;

    private float _rotationSpeed = 10f;
    private float _agentSpeed = 6;

    // 애니메이션 끝났을 때 호출
    public Action<bool> OnStateChanged;

    private System.Random _random = new System.Random();
    #endregion

    protected override void Init()
	{
        base.Init();

        SetLayerRecursively(this.gameObject, LayerMask.NameToLayer("Monster"));

        AddComponent();

        TriggerEnvironmentEvent();

        InitHpBar();

        UnActiveShaderXRay();

        StartCoroutine(coAppearActive());
    }

    protected override void UpdateController()
    {
        if (State == CreatureState.Dead)
            return;

        Moving();
    }

    private void Moving()
    {
        if (_agent != null)
        {
            Transform root = transform.parent != null ? transform.parent : transform;

            Vector3 newPosition = _agent.nextPosition;
            root.position = newPosition;
            CellPos = newPosition;

            if (_agent.desiredVelocity.sqrMagnitude > 0.01f)
            {
                _targetRotation = Quaternion.LookRotation(_agent.desiredVelocity);
            }

            if (Quaternion.Angle(root.rotation, _targetRotation) > 0.5f)
            {
                root.rotation = Quaternion.Slerp(root.rotation, _targetRotation, Time.deltaTime * _rotationSpeed);
                RotInfo = _targetRotation;
            }
        }
    }
    public override void OnDamaged() {}

    public void OnHit(S_AttackInfo atkInfoPacket)
    {
        BaseController tbc = Managers.Object.FindById(atkInfoPacket.ObjectId)?.GetComponentInChildren<BaseController>();
        if (tbc == null || tbc == this)
            return;

        GameObjectType targetType = ObjectManager.GetObjectTypeById(tbc.Id);
        GameObjectType atkType = ObjectManager.GetObjectTypeById(atkInfoPacket.AttackerId);
        if (targetType == atkType)
            return;

        Vector3 targetPosition = tbc.transform.position;

        if (Sound != null)
        {
            Sound.GetRandom3DEffect($"{atkInfoPacket.AttackType}_Hit", targetPosition);
        }

        if (DataManager.MonsterEffectDict.TryGetValue(Skill, out List<EffectData> data))
        {
            List<EffectData> hitEffects = data.Where(effect =>
                !string.IsNullOrEmpty(effect.prefabName) &&
                effect.prefabName.IndexOf("Hit", StringComparison.OrdinalIgnoreCase) >= 0
            ).ToList();

            if (hitEffects.Count > 0)
                Managers.FX.PlayEffect(atkInfoPacket.AttackerId, hitEffects, tbc.transform, TargetPosition, TargetPosition);
        }
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
        _agent?.ResetPath();

        OnDead(); 
    }

    public void OnIdlePacket(S_State packet)
    {
        if (_agent != null)
        {
            _agent.SetDestination(packet.PosInfo.ToVector()); 
        }

        if (packet.RotInfo != null)
        {
            _targetRotation = new Quaternion(packet.RotInfo.Qx, packet.RotInfo.Qy, packet.RotInfo.Qz, packet.RotInfo.Qw);
        }
    }

    public void OnMovePacket(S_State packet)
    {
        if (_agent != null)
         {    
            _agent.SetDestination(packet.PosInfo.ToVector());
        }

        if(packet.RotInfo != null)
            _targetRotation = new Quaternion(packet.RotInfo.Qx, packet.RotInfo.Qy, packet.RotInfo.Qz, packet.RotInfo.Qw);
    }

    public void OnSkillPacket(S_State packet)
    {
        Skill = packet.Skilltype;

        if (Type == MonsterType.Drone || Type == MonsterType.Turret)
            OnStateChanged?.Invoke(false);
        else
            OnStateChanged?.Invoke(true);
        
        if (_agent != null)
        {
           _agent.ResetPath();
            _agent.SetDestination(packet.PosInfo.ToVector());
        }

        if (packet.RotInfo != null)
            _targetRotation = new Quaternion(packet.RotInfo.Qx, packet.RotInfo.Qy, packet.RotInfo.Qz, packet.RotInfo.Qw);
    }

    private void ChangeTransformInfo(S_State packet)
    {
        if (packet.RotInfo != null)
            _targetRotation = new Quaternion(packet.RotInfo.Qx, packet.RotInfo.Qy, packet.RotInfo.Qz, packet.RotInfo.Qw);
    }
    private bool CheckBehaviorCondition(CreatureState nextState)
    {
        if (State == CreatureState.Appear && nextState == CreatureState.Idle)
            return true;

        if (Type == MonsterType.Omega && (State == CreatureState.Skill && nextState == CreatureState.Idle))
            return true;

        return false;
    }
    public void OnRecvStatePacket(S_State packet)
    {
        if (!packet.ChangeState)
        {
            ChangeTransformInfo(packet);
            return;
        }

        if (CheckBehaviorCondition(packet.MyState))
        {
            OnStateChanged?.Invoke(true);
        }

        if (packet.TargetPosition != null)
        {
            TargetPosition = packet.TargetPosition.ToVector();
        }

        State = packet.MyState;
        Skill = MonsterSkill.MsNone;

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

    // todo* Appear Animation
    private IEnumerator coAppearActive()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        yield return new WaitForSeconds(0.3f);

        foreach (var renderer in renderers)
        {
            IsHide = false;
            renderer.enabled = true;
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

    private bool AddComponent()
    {
        State = CreatureState.Appear;

        // NavMeshAgent
        _agent = GetComponentInParent<NavMeshAgent>();
        if (_agent != null)
        {
            _agent.updatePosition = false;
            _agent.updateRotation = false;
            _agent.speed = _agentSpeed;
            SyncPos(true);
        }
        _targetRotation = transform.rotation;

        // VisualEffectController
        _highlightEffect = gameObject.AddComponent<VisualEffectController>();
        _highlightEffect.Owner = this;

        // Animator
        if (_animator == null)
            return false;
        _animator.applyRootMotion = false;

        // Rotation Speed
        if (Type == MonsterType.Gamma || Type == MonsterType.Omega)
            _rotationSpeed = 10.0f;
        else
            _rotationSpeed = 40.0f;

        Sound = gameObject.GetOrAddComponent<SoundController>();
        if (Sound != null)
        {
            Sound.PreloadMonsterAllSounds(Type);
        }

        // Renderer
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            renderer.enabled = false;
        }

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
            return;
        }

        ui.SetTarget(gameObject);
        UpdateMaxHp();
        UpdateHp();
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

    private void TriggerEnvironmentEvent()
    {
        if (Type == MonsterType.Gamma)
        {
            GameObject targetObject = GameObject.Find("BarrierSpawnpoint");
            if (targetObject != null)
            {
                Env_BarrierSpawnpoint component = targetObject.GetComponent<Env_BarrierSpawnpoint>();
                component.ActivatePhase2();
            }
        }
    }
    #endregion
}

