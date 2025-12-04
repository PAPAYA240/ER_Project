using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Data;
using Google.Protobuf.Protocol;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using static Data.SkillEffectList;

public class PlayerController : CreatureController
{
    bool _isKeyInput = false;
    int _atkCount = 1;
    int _maxAtkCount = 2;

    // SyncPos
    float _minDist = 3f;
    float _syncSpeed = 20f;
    Vector3 _serverPos;
    float AGENT_SPEED_RATIO = 1.7f;

    // Fog
    private FogOfWarVision _fogOfWarVision;

    protected bool _isSkillDebug = true;
    protected bool _isRest = false;
    public bool AllowOffPathMovement { get; set; } = false;

    // NameTag
    protected UI_PlayerNameTag _nameTag;
    public UI_PlayerNameTag NameTag { get { return _nameTag; } }

    public string NickName { get; set; } = "UserName";

    // 장착 아이템
    private Dictionary<EquipItemType, EquipItemInfo> _equipItemSlot = new Dictionary<EquipItemType, EquipItemInfo>();
    public Dictionary<EquipItemType, EquipItemInfo> EquipItemSlot { get { return _equipItemSlot; } }
    public ItemStat ItemStat { get; private set; } = new ItemStat();

    // 애니메이션 관련
    protected GameObject _eqipWeapon = null;
    protected GameObject _restItem = null;
    protected Animator _weaponAnimator = null;

    public SoundController Sound;

    // 유키 스킬 이펙트
    public SkillEffectHandler YukiEffects { get; private set; } = new SkillEffectHandler();

    // Kill Count : 20초 안에 얼만큼의 처치했는는가?
    public float CurrentMultiKillCnt
    {
        get { return _currentMultiKillCount;  }
        set { ++_currentMultiKillCount; }
    }
    private const float _multiKillTimeLimit = 20.0f;
    private float _currentMultiKillCount = 0;
    private float _lastKillTime = 0.0f;

    #region Property
    public override float Attack
    {
        get { return base.Attack; }
        set { base.Attack = value; }
    }

    public float AttackSpeed
    {
        get { return Stat.AttackSpeed; }
        set { Stat.AttackSpeed = value; }
    }

    public override float Defense
    {
        get { return base.Defense; }
        set { base.Defense = value; }
    }

    public float CriticalRatio { get { return Mathf.Min(ItemStat.CriticalRatio, 1f); } }

    public virtual float Healing
    {
        get { return Stat.Healing; }
        set { Stat.Healing = value; }
    }

    public override float Hp
    {
        get { return base.Hp; }
        set { Stat.Hp = Math.Clamp(value, 0, MaxHp); UpdateHp(); }
    }

    public override float MaxHp
    {
        get { return base.MaxHp + ItemStat.MaxHp + ItemStat.MaxHpPerLevel * Stat.Level; }
        set { base.MaxHp = value; }
    }

    public override float HpRegen
    {
        get { return base.HpRegen * (1 + ItemStat.HpRegen); }
        set { Stat.HpRegen = Math.Max(value, 0); }
    }

    public override float MaxStamina
    {
        get { return base.MaxStamina + ItemStat.MaxStamina; }
        set { base.MaxStamina = value; }
    }

    public override float Stamina
    {
        get { return base.Stamina; }
        set { Stat.Stamina = Math.Clamp(value, 0, MaxStamina); UpdateStamina(); }
    }

    public override float StaminaRegen
    {
        get { return base.StaminaRegen * (1 + ItemStat.StaminaRegen); }
        set { Stat.StaminaRegen = Math.Max(value, 0); }
    }

    public float SkillAmplification
    {
        get
        {
            return (ItemStat.FixedSkillAmplification + ItemStat.SkillAmplificationPerLevel * Stat.Level + AdaptiveStat)
                * (1 + ItemStat.PercentageSkillAmplification);
        }
    }

    public override float Speed
    {
        get { return Stat.MoveSpeed; }
        set { Stat.MoveSpeed = value; _agent.speed = value * AGENT_SPEED_RATIO; }
    }

    public override float FixedDefensePenetration { get { return ItemStat.FixedDefensePenetration; } }
    public override float PercentageDefensePenetration { get { return ItemStat.PercentageDefensePenetration; } }

    public float AdaptiveStat
    {
        get
        {
            if (ItemStat.AdaptiveStat == 0)
                return 0;

            float att, skillamp;
            att = ItemStat.AttackDamage + ItemStat.AttackDamagePerLevel * Stat.Level;
            skillamp = (ItemStat.FixedSkillAmplification + ItemStat.SkillAmplificationPerLevel * Stat.Level)
                * (1 + ItemStat.PercentageSkillAmplification);

            if (att * 2 > skillamp)
                return ItemStat.AdaptiveStat;
            else
                return ItemStat.AdaptiveStat * 2;
        }
    }

    private bool _untargetable;
    public override bool Untargetable 
    { 
        get => _untargetable; 
        set 
        {
            if (_untargetable == value)
                return;

            _untargetable = value;

            if (_untargetable)
                _nameTag.SetUntargetable();
            else
                _nameTag.SetNameText(ObjInfo.Player.Nickname, 16);

            _nameTag.SetHPColor(_untargetable);
        } 
    }
    private bool _unstoppable;
    public override bool Unstoppable 
    {
        get { return _unstoppable; }
        set 
        {
            if (_unstoppable == value)
                return;

            _unstoppable = value;

            if (_unstoppable)
                _nameTag.SetUnstoppable();
            else
                _nameTag.SetNameText(ObjInfo.Player.Nickname, 16);
        } 
    }
    #endregion

    // 레이어
    protected string layerName;

    // 화살
    protected Transform _equipTransform = null;

    // Bush Material
    private Dictionary<Renderer, Material[]> _originalMaterialsDict = new Dictionary<Renderer, Material[]>();
    private Material _playerBushMaterial;
    private Transform _lodTransform;

    public bool HidingInBush = false;
    #region KDA

    public int KillAmount { get; private set; } = 0; 
    public int DeathAmount { get; private set; } = 0; 
    public int AsistAmount { get; private set; } = 0; 

    public virtual void SetKDA(int Kiil,int Death,int Asist)
    {
        KillAmount = Kiil;
        DeathAmount = Death;
        AsistAmount = Asist;

        // UI에 알리는 코드 필요할 듯.
    }

    #endregion

    CombatState _combatMode;
    public virtual CombatState CombatStat
    {
        get { return _combatMode; }
        set { _combatMode = value; }
    }


    public bool IsKeyInput
    {
        get { return _isKeyInput; }
        set
        {
            _isKeyInput = value;
        }
    }

    public int AttackCount
    {
        get { return _atkCount; }
        set { _atkCount = value; }
    }

    public int MaxAttackCount
    {
        get { return _maxAtkCount; }
        set { _maxAtkCount = value; }
    }

    public bool IsRest
    {
        get { return _isRest; }
        set { _isRest = value; }
    }

    protected override void Init()
    {
        base.Init();

        this.gameObject.layer = LayerMask.NameToLayer("Player");

        // Fog
        GameObject go = new GameObject();
        go.name = "FogOfWarVision";
        go.transform.parent = transform;
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        go.AddComponent<FogOfWarVision>();
        string layerName = $"FogTeam{ObjInfo.Player.Team}";
        go.layer = LayerMask.NameToLayer(layerName);

        // 체력바
        InitNameTag();

        // 유키용
        YukiEffects.InitEffects(this);

        // Chat
        GameObject goChat = Managers.Resource.Instantiate("UI/Chat/ChatBackground");
        goChat.transform.SetParent(gameObject.transform);

        // 장비 슬롯
        InitEquipItem();
        InitializeXRay();
        InitBushRenderSetting();

        // NavMesh Agent
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = Speed * AGENT_SPEED_RATIO;
        _agent.acceleration = 999;
        _agent.angularSpeed = 720;
        _agent.stoppingDistance = 0.1f;

        // Sound
        Sound = gameObject.GetOrAddComponent<SoundController>();
        if (Sound != null)
            Sound.PreloadCharAllSounds(ObjInfo.Player.CharType);

        // Rest Item
        RegisterRestItem();

        // Weapon Anim
        RegisterWeaponAnimator();           
    }

    private void InitEquipItem()
    {
        for (int i = 0; i < (int)EquipItemType.End; ++i)
        {
            _equipItemSlot.Add((EquipItemType)i, new EquipItemInfo());
        }

        EquipWeapon();
    }

    private void EquipWeapon()
    {
        if (ObjInfo.Player.CharType == CharacterType.Theodore)
        {
            Transform RTransform = Util.FindChildByName(transform, "Equip_R").transform;

            // 스나이퍼
            _eqipWeapon = Managers.Resource.Instantiate($"Creature/Weapon/WP_Theodore_SP01_Sniperrifle_LOD");
            if (_eqipWeapon != null)
            {
                if (RTransform != null)
                {
                    _eqipWeapon.gameObject.AddComponent<WeaponController>();

                    _eqipWeapon.transform.SetParent(RTransform);
                    _eqipWeapon.transform.localPosition = Vector3.zero;
                    _eqipWeapon.transform.localRotation = Quaternion.identity;
                    _eqipWeapon.transform.localScale = Vector3.one;
                }
            }
        }
    }

    public void ManualInit()
    {
        Init();
    }

    void Start()
    {

    }

    protected override void UpdateController()
    {
        base.UpdateController();
        MultiKillTimer();

        if (Id != Managers.Object.MyPlayer.Id)
        {
            float dist = Vector3.Distance(transform.position, _serverPos);
            if (dist > _minDist)
            {
                if (_agent == null || !_agent.isOnNavMesh)
                    return;
                _agent.Warp(_serverPos);
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, _serverPos, Time.deltaTime * _syncSpeed);
            }
        }
    }

    
    protected virtual void CheckUpdatedFlag() { }

    public override void OnDamaged()
    {
    }

    public void OnHit(S_AttackInfo atkInfoPacket)
    {
        BaseController tbc = Managers.Object.FindById(atkInfoPacket.ObjectId)?.GetComponentInChildren<BaseController>();
        if (tbc == null)
            return;
        Vector3 targetPosition = tbc.transform.position;

        // 사용 중인 키(Player)/몬스터 스킬(Monster) 이름 + hit
        // ex. Q_Hit, W_Hit, 
        if (Sound != null)
            Sound.GetRandom3DEffect($"{atkInfoPacket.AttackType}_Hit", targetPosition);

        if (Enum.TryParse<KeyCode>(atkInfoPacket.AttackType, out KeyCode key))
            PlaySelectEffect(key, default(Vector3), default(Vector3), default(Quaternion), $"FX_{key}_Hit", tbc.transform);

    }

    public void OnStop(S_Stop packet)
    {
        if (_agent == null || !_agent.isOnNavMesh)
            return;

        _agent.isStopped = true;
        _agent.ResetPath();
    }

    public void OnRespawn(S_Respawn packet)
    {
        _serverPos = transform.position = packet.PosInfo.ToVector();
        _agent.Warp(new Vector3(packet.PosInfo.PosX, packet.PosInfo.PosY, packet.PosInfo.PosZ));
        Hp = packet.Hp;
    }

    public void ChangeState(S_PlayerState packet)
    {
        State = packet.State;
    }

    public void ChangeStatus(S_ChangeStatus packet)
    {
        Speed = packet.MoveSpeed;
        Attack = packet.Attack;
        AttackSpeed = packet.AttackSpeed;
        Defense = packet.Defense;
        Healing = packet.Healing;
    }

    public void ChangeAttackRange(S_ChangeAttackRange packet)
    {
        AttackRange = packet.AttackRange;
    }

    #region Util
    public Vector3 GetMouseWorldPosition()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) return Vector3.zero;

        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Map")))
            return hit.point;

        return Vector3.zero;
    }
    protected string GetCharacterName()
    {
        return System.Enum.GetName(typeof(CharacterType), ObjInfo.Player.CharType);
    }
    #endregion

    #region Animation
    protected virtual void PlayAnimation(string animName, float ratio)
    {
        int layerIndex = _animator.GetLayerIndex(layerName);
        if (layerIndex == -1)
            return;

        _animator.CrossFadeInFixedTime(animName, ratio);
    }

    
    public void PlayAnimFromServer(AnimInfo animInfo)
    {
        PlayAnimationSound(animInfo.Name);

        bool isUpperBodySkill = animInfo.Name == "ROZZI_D" || animInfo.Name == "YUKI_W";
        if (isUpperBodySkill)
        {
            int upperLayer = _animator.GetLayerIndex("UpperBody");
            _animator.CrossFadeInFixedTime(animInfo.Name, animInfo.Ratio, upperLayer);
            return;
        }

        AnimCondition(animInfo.Name);

        _animator.CrossFadeInFixedTime(animInfo.Name, animInfo.Ratio);

        if (animInfo.IsChangeSpeed == true)
            _animator.SetFloat("AttackSpeed", animInfo.Speed);

        WeaponAnim(animInfo.Name, animInfo.Ratio, animInfo.IsChangeSpeed, animInfo.Speed);
    }

    private void AnimCondition(string name)
    {
        if (ObjInfo.Player.CharType == CharacterType.Theodore)
        {
            // *todo. operate 조건이 자꾸 true로 만들어서 애니메이션으로 조정
            if (name == "OPERATE" && _eqipWeapon.gameObject.activeInHierarchy == true)
            {
                _eqipWeapon.gameObject.SetActive(false);
            }
            else if (_eqipWeapon.gameObject.activeInHierarchy == false)
            {
                _eqipWeapon.gameObject.SetActive(true);
                ActiveRenderer(true);
            }

            if (name == "REST_START" || name == "REST_LOOP")
                RenderRestItem(true);
            else
                RenderRestItem(false);
        }
        else if(ObjInfo.Player.CharType == CharacterType.Abigail)
        {
            if (name == "REST_START" || name == "REST_LOOP")
                RenderRestItem(true);
            else
                RenderRestItem(false);
        }
    }
    private int GetAnimationLayer(string animName)
    {
        int layerCount = _animator.layerCount;
        for (int i = 0; i < layerCount; i++)
        {
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(i);

            if (stateInfo.IsName(animName))
                return i;
        }

        return -1;
    }
    public void ChangeSpeed(string paramName, float speed)
    {
        _animator.SetFloat(paramName, speed);
    }

    public void PlayEffectFromServer(S_Fx packet, Vector3 mousePos, Vector3 targetPos = new Vector3(), Quaternion targetRot = default(Quaternion))
    {
        Transform targetTransform = null;
        if(packet.UseTargetTransform)
        {
            if (packet.TargetId == 0)
                return;

            GameObject go = Managers.Object.FindById(packet.TargetId);
            if (go == null)
                return;

            targetTransform = go.transform;
        }

        if(!packet.IsCommon)
        {
            if (packet.Type == "Caster")
                PlaySkillEffect((KeyCode)packet.SkillKey, mousePos, targetPos, targetRot, targetTransform: targetTransform);
            else if (packet.Type == "Select")
                PlaySelectEffect((KeyCode)packet.SkillKey, mousePos, targetPos, targetRot, packet.FxName, targetTransform: targetTransform);
        }
        else
        {
            if (packet.Type == "Caster")
                PlayCommonCasterEffect(packet.CommonName, mousePos, targetPos, targetRot);
            else if(packet.Type == "Select")
                PlayCommonSelectEffect(packet.CommonName, packet.FxName, mousePos, targetPos, targetRot);
        }
    }
    #endregion

    #region Sound
    private void PlayAnimationSound(string name)
    {
        // Animation에 맞는 Sound
        if (Sound == null)
            return;

        if (name == "RUN")
        {
            if (_runSoundCoroutine == null)
                _runSoundCoroutine = StartCoroutine(FootStepSound());
        }
        else
        {
            if (_runSoundCoroutine != null)
            {
                StopCoroutine(_runSoundCoroutine);
                _runSoundCoroutine = null;
            }

            Sound.GetEffect3D(name, transform.position); 
            Sound.GetRandom3DVoice(name, transform.position);
        }
    }

    Coroutine _runSoundCoroutine = null;
    private float _footstepTimer = 0.4f;
    private float _footstepInterval = 0.4f;
    protected IEnumerator FootStepSound()
    {
        _footstepTimer = 0.4f;
        while (true)
        {
            _footstepTimer -= Time.deltaTime;
            if (_footstepTimer <= 0)
            {
                Sound.GetRandom3DEffect("FootStep", transform.position);
                _footstepTimer = _footstepInterval;
            }
            yield return null;
        }
    }

    #endregion
    public void LookAtMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100f))
        {
            Vector3 targetPoint = hit.point;
            targetPoint.y = transform.position.y;
            Vector3 direction = targetPoint - transform.position;

            if (direction != Vector3.zero)
            {
                Quaternion newRotation = Quaternion.LookRotation(direction);
                RotInfo = newRotation;
                SyncPos(true);
            }
        }
    }

    public Vector2 GetMousePos()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100f))
        {
            Vector3 targetPoint = hit.point;
            return new Vector2(targetPoint.x, targetPoint.z);
        }
        return Vector2.zero;
    }

    public void LookAtMouse(Vector2 mousePos)
    {
        Vector3 casterPosition = transform.position;

        Vector3 targetPoint = new Vector3(mousePos.x, casterPosition.y, mousePos.y);

        Vector3 direction = targetPoint - casterPosition;

        if (direction != Vector3.zero)
        {
            Quaternion newRotation = Quaternion.LookRotation(direction);

            RotInfo = newRotation;
            SyncPos(true);
        }
    }
    Dictionary<KeyCode, SkillMesh> msDict = new Dictionary<KeyCode, SkillMesh>();

    #region NameTagAndHp
    protected void InitNameTag()
    {
        GameObject go = null;

        if(ObjInfo.Player.CharType == CharacterType.Yuki)
        {
            go = Managers.Resource.Instantiate("UI/SubItem/YukiNameTagCanvas", gameObject.transform);
        }
        else
        {
            go = Managers.Resource.Instantiate("UI/SubItem/PlayerNameTagCanvas", gameObject.transform);
        }

        if (null == go)
        {
            Debug.Log("go is null : InitNameTag()");
            return;
        }

        _nameTag = go.GetComponentInChildren<UI_PlayerNameTag>();
        if (null == _nameTag)
        {
            Debug.Log("_nameTag is null : InitNameTag()");
            return;
        }

        go.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        _nameTag.SetTarget(gameObject);
        _nameTag.SetHPColor();
        _nameTag.SetNameText(ObjInfo.Player.Nickname, 16);

        //이거 왜 터지지?
        _nameTag.SetLevelText(Stat.Level);
        UpdateHp();
        UpdateMaxHp();
        UpdateStamina();
        UpdateMaxStamina();
    }
    protected override void UpdateHp()
    {
        if (_nameTag == null)
            return;
        _nameTag.SetHp(Hp);
    }
    protected override void UpdateMaxHp()
    {
        if (_nameTag == null)
            return;
        _nameTag.SetMaxHp(MaxHp);
    }

    protected override void UpdateBarrier()
    {
        if (_nameTag == null)
            return;
        _nameTag.SetBarrier(Barrier);
    }
    protected override void UpdateStamina()
    {
        if (_nameTag == null)
            return;
        _nameTag.SetStamina(Stamina);
    }
    protected override void UpdateMaxStamina()
    {
        if (_nameTag == null)
            return;
        _nameTag.SetMaxStamina(MaxStamina);
    }

    public void SetNameTagLevel()
    {
        if (_nameTag == null)
            return;
        _nameTag.SetLevelText(Stat.Level);
    }

    #endregion

    #region Item
    public virtual void UpdateItemStat(ItemStat stat)
    {
        ItemStat = stat;
        UpdateHp();
        UpdateMaxHp();
        UpdateStamina();
        UpdateMaxStamina();
    }

    public virtual void EquipItem(int itemId)
    {
        //TODO 아이템 도감에서 아이템을 가져와서 처리(+UI도)
        EquipItemInfo item = DataManager.ItemDict[itemId] as EquipItemInfo;
        _equipItemSlot[item.Type] = item;
    }

    #endregion

    #region Effect
    // 기본 스킬 이펙트 호출 : Caster Type
    public void PlaySkillEffect(KeyCode skillKey, Vector3 mousePos, Vector3 targetPos, Quaternion targetRot = default(Quaternion), Transform targetTransform = null)
    {
        CharacterType type = ObjInfo.Player.CharType;
        CreatureState state = CreatureState.Skill;

        if (!DataManager.PlayerFxDict.ContainsKey(type))
            return;
        if (!DataManager.PlayerFxDict[type].ContainsKey(state))
            return;
        if (!DataManager.PlayerFxDict[type][state].ContainsKey(skillKey))
            return;

        SkillEffectList myEffectList = DataManager.PlayerFxDict[type][state][skillKey];
        List<EffectData> dataList = new List<EffectData>();
        foreach (EffectData effect in myEffectList.Caster)
        {
            dataList.Add(effect);
        }

        Managers.FX.PlayEffect(ObjInfo.ObjectId, dataList, targetTransform ? targetTransform : transform, mousePos);
    }

    // 직접 선택해서 호출하는 이펙트 : Type Select
    public void PlaySelectEffect(KeyCode skillKey, Vector3 mousePos, Vector3 targetPos, Quaternion targetRot, string fxName, Transform targetTransform = null)
    {
        CharacterType type = ObjInfo.Player.CharType;
        CreatureState state = CreatureState.Skill;

        if (!DataManager.PlayerFxDict.ContainsKey(type))
            return;
        if (!DataManager.PlayerFxDict[type].ContainsKey(state))
            return;
        if (!DataManager.PlayerFxDict[type][state].ContainsKey(skillKey))
            return;

        SkillEffectList myEffectList = DataManager.PlayerFxDict[type][state][skillKey];
        if (myEffectList?.Select == null)
            return;

        List<EffectData> dataList = myEffectList.Select
       .Where(effect => effect != null && effect.prefabName == fxName)
       .ToList();

        if (dataList.Count == 0)
            return;

        Managers.FX.PlayEffect(ObjInfo.ObjectId, dataList, targetTransform ? targetTransform : transform, mousePos, targetPos, targetRot);
    }

    // 공통 이펙트 : Type Common - Caster
    public void PlayCommonCasterEffect(string commonName, Vector3 mousePos, Vector3 targetPos, Quaternion targetRot, Transform targetTransform = null)
    {
        if (DataManager.CommonFxDict == null)
            return;

        if (!DataManager.CommonFxDict.TryGetValue(commonName, out SkillEffectList effectList))
            return;

        var dataList = new List<EffectData>();
        if (effectList.Caster != null)
            dataList.AddRange(effectList.Caster);

        Managers.FX.PlayEffect(ObjInfo.ObjectId, dataList, targetTransform ? targetTransform : transform, mousePos, targetPos, targetRot, isCommon: true);
    }

    // 공통 이펙트 : Type Common - Select
    public void PlayCommonSelectEffect(string commonName, string fxName, Vector3 mousePos, Vector3 targetPos, Quaternion targetRot, Transform targetTransform = null)
    {
        if (DataManager.CommonFxDict == null)
            return;

        if (!DataManager.CommonFxDict.TryGetValue(commonName, out SkillEffectList effectList))
            return;

        var dataList = new List<EffectData>();

        if (effectList.Select != null)
        {
            foreach (EffectData effect in effectList.Select)
            {
                if (effect.prefabName == fxName)
                    dataList.Add(effect);
            }
        }

        if (dataList.Count == 0)
            return;

        Managers.FX.PlayEffect(ObjInfo.ObjectId, dataList, targetTransform ? targetTransform : transform, mousePos, targetPos, targetRot, isCommon: true);
    }
    #endregion

    #region State:Operate
    public IEnumerator CoRotateToPosition(Vector3 targetPos)
    {
        float rotateSpeed = 15f;
       
        while (true)
        {
            if (State == CreatureState.Moving)
                break;

            Vector3 dir = targetPos - transform.position;
            dir.y = 0;

            if (dir.magnitude < 0.1f)
                break;

            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotateSpeed);

            if (Quaternion.Angle(transform.rotation, targetRot) < 1f)
                break;

            yield return null;
        }
    }
    #endregion

    [Header("X-Ray Settings")]
    [SerializeField] private int xRayIgnoreStencilID = 100;
    #region Shader
    void InitializeXRay()
    {
        SetupPlayerWeaponXRay();
    }

    void SetupPlayerWeaponXRay()
    {
        // Player 본체
        SetXRayGroup(gameObject, xRayIgnoreStencilID);

        if (_eqipWeapon != null)
            SetXRayGroup(_eqipWeapon, xRayIgnoreStencilID);
    }
    public void SetxRayFromPlayer(GameObject player)
    {
         SetXRayGroup(player, xRayIgnoreStencilID);
    }
    void SetXRayGroup(GameObject root, int stencilID)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            foreach (Material mat in renderer.materials)
            {
                if (mat.shader.name.Contains("Toon_DoubleShadeWithFeather"))
                {
                    if (mat.HasProperty("_StencilRef"))
                    {
                        mat.SetInt("_StencilRef", stencilID);
                        mat.SetInt("_StencilComp", (int)UnityEngine.Rendering.CompareFunction.Always);
                        mat.SetInt("_StencilOp", (int)UnityEngine.Rendering.StencilOp.Replace);
                    }
                }
            }
        }
    }

    // 무기 교체 시 호출 (기존 EquipWeapon 메서드에 추가)
    void OnWeaponEquipped(GameObject newWeapon)
    {
        if (newWeapon != null)
        {
            SetXRayGroup(newWeapon, xRayIgnoreStencilID);
        }
    }

    // Player와 Weapon의 X-Ray 효과 끄기/켜기
    public void SetPlayerWeaponXRayEnabled(bool enabled)
    {
        float alpha = enabled ? 0.5f : 0f;

        SetOccludedColorAlpha(gameObject, alpha);

        if (_eqipWeapon != null)
        {
            SetOccludedColorAlpha(_eqipWeapon, alpha);
        }
    }

    void SetOccludedColorAlpha(GameObject root, float alpha)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            foreach (Material mat in renderer.materials)
            {
                if (mat.HasProperty("_OccludedColor"))
                {
                    Color occludedColor = mat.GetColor("_OccludedColor");
                    occludedColor.a = alpha;
                    mat.SetColor("_OccludedColor", occludedColor);
                }
            }
        }
    }
    void SetRenderingLayerMask(GameObject root, uint layerMask)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            renderer.renderingLayerMask = layerMask;
        }
    }
    void SetupRenderingLayer()
    {
        uint playerLayer = 1u << 1; // Layer 1

        SetRenderingLayerMask(gameObject, playerLayer);

        if (_eqipWeapon != null)
        {
            SetRenderingLayerMask(_eqipWeapon, playerLayer);
        }
    }

   
    #endregion
    public void SyncPosFromServer(S_Move movePacket)
    {
        if (_agent == null || !_agent.isOnNavMesh)
            return;

        _agent.isStopped = false;

        _serverPos = new Vector3
        {
            x = movePacket.PosInfo.PosX,
            y = movePacket.PosInfo.PosY,
            z = movePacket.PosInfo.PosZ
        };

        transform.rotation = movePacket.RotInfo;
    }

    public void SyncPosFromServer(PositionInfo positionInfo, RotationInfo rotationInfo)
    {
        if (_agent == null || !_agent.isOnNavMesh)
            return;

        _agent.isStopped = false;

        _serverPos = new Vector3
        {
            x = positionInfo.PosX,
            y = positionInfo.PosY,
            z = positionInfo.PosZ
        };

        transform.rotation = rotationInfo;
    }
    private void MultiKillTimer()
    {
        if (CurrentMultiKillCnt <= 0)
            return;

        _lastKillTime += Time.deltaTime;
        if (_multiKillTimeLimit <= _lastKillTime)
        {
            CurrentMultiKillCnt = 0;
            return;
        }
        return;
    }

    #region Bush Renderer
    private void InitBushRenderSetting()
    {
        _playerBushMaterial = Resources.Load<Material>("Material/ghostMaterial");

        foreach (Transform child in transform)
        {
            if (child.name.Contains("LOD"))
            {
                _lodTransform = child;
                break;
            }
        }

        if (_lodTransform != null)
        {
            Renderer[] renderers = _lodTransform.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                _originalMaterialsDict[renderer] = renderer.materials;
            }
        }
    }
    Coroutine _coRenderer = null;
    public void ActiveRenderer(bool active, float duration = 0f)
    {
        if (active == false)
        {
            MakeInvisible();
        }
        else
        {
            if (_coRenderer != null)
                StopCoroutine(_coRenderer);

            _coRenderer = StartCoroutine(MakeVisible(duration));
        }
    }

    // 렌더러 비활성화
    private void MakeInvisible()
    {
        if (_lodTransform == null)
            return;

        HidingInBush = true;
        _nameTag.gameObject.SetActive(false);

        Renderer[] renderers = _lodTransform.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = false;
        }
    }

    // 렌더러 활성화
    private IEnumerator MakeVisible(float duration = 0f)
    {
        if (_lodTransform == null)
            yield break;

        yield return new WaitForSeconds(duration);
        HidingInBush = false;
        _nameTag.gameObject.SetActive(true);

        Renderer[] renderers = _lodTransform.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = true;

            // 각 Renderer의 원본 Material 복원
            if (_originalMaterialsDict.TryGetValue(renderer, out Material[] originalMaterials))
            {
                renderer.materials = originalMaterials;
            }
        }
    }

    public void ChangeBushRenderer()
    {
        if (_lodTransform == null)
            return;

        HidingInBush = true;
        Renderer[] renderers = _lodTransform.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = true;

            int materialCount = renderer.sharedMaterials.Length;
            Material[] ghostMaterials = new Material[materialCount];
            for (int i = 0; i < materialCount; i++)
            {
                ghostMaterials[i] = _playerBushMaterial;
            }
            renderer.sharedMaterials = ghostMaterials;
        }
    }
    #endregion

    #region State: Rest
    void RegisterRestItem()
    {
        if(ObjInfo.Player.CharType == CharacterType.Abigail)
        {
            foreach (Transform child in transform.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "AbigailTable")
                {
                    _restItem = child.gameObject;
                    RenderRestItem(false);
                    return;
                }
            }
        }
        else if (ObjInfo.Player.CharType == CharacterType.Theodore)
        {
            foreach (Transform child in transform.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "RestBox")
                {
                    _restItem = child.gameObject;
                    RenderRestItem(false);
                    return;
                }
            }
        }
    }

    public void RenderRestItem(bool render)
    {
        if (_restItem == null)
            return;
        _restItem.SetActive(render);
    }
    #endregion

    #region WeaponAnim
    void RegisterWeaponAnimator()
    {
        if (ObjInfo.Player.CharType == CharacterType.Abigail)
        {
            Transform weaponTransform = GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t.name == "AbigailWeapon");
            if (weaponTransform != null)
            {
                // AbigailWeapon의 자식에서 Animator 찾기
                _weaponAnimator = weaponTransform.GetComponentInChildren<Animator>();

                if (_weaponAnimator == null)
                    Debug.LogWarning("AbigailWeapon 자식에서 Animator를 찾을 수 없습니다.");
            }
        }
    }

    void WeaponAnim(string animName, float transDuration, bool speedChanged, float speed)
    {
        if (_weaponAnimator == null)
            return;

        if(speedChanged)
            _weaponAnimator.SetFloat("AttackSpeed", speed);

        if(animName == "SKILL_T" || animName == "SKILL_Q" || animName == "SKILL_W")
            _weaponAnimator.CrossFadeInFixedTime(animName, transDuration);
        else
            _weaponAnimator.CrossFadeInFixedTime("WAIT", transDuration);
    }
    #endregion
}
