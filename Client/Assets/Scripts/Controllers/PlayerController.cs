using Data;
using Google.Protobuf.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
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

    // MoveSync
    private float minDiff = 0.2f;

    // Fog
    private FogOfWarVision _fogOfWarVision;

    protected bool _isSkillDebug = true;
    // NameTag
    protected UI_PlayerNameTag _nameTag;
    public UI_PlayerNameTag NameTag { get { return _nameTag; } }

    // 장착 아이템
    Dictionary<EquipItemType, EquipItemInfo> _equipItemSlot = new Dictionary<EquipItemType, EquipItemInfo>();
    public ItemStat ItemStat { get; private set; } = new ItemStat();
    protected GameObject _eqipWeapon = null;

    #region Property
    public override float Attack
    {
        get { return base.Attack + ItemStat.AttackDamage + ItemStat.AttackDamagePerLevel * Stat.Level + AdaptiveStat; }
        set { base.Attack = value; }
    }

    public override float Defense
    {
        get { return base.Defense + ItemStat.Defense; }
        set { base.Defense = value; }
    }

    public float CriticalRatio { get { return Mathf.Min(ItemStat.CriticalRatio, 1f); } }

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
        get { return (Stat.MoveSpeed + ItemStat.FixedSpeed) * (1 + ItemStat.PercentageSpeed) * 1.7f; }
        set { Stat.MoveSpeed = value; }
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

    #endregion

    // 레이어
    protected string layerName;

    // 화살
    protected GameObject _projectile = null;
    protected Transform _equipTransform = null;

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


    public bool IsKeyInput
    {
        get { return _isKeyInput; }
        set
        {
            _isKeyInput = value;
            Debug.Log($"IsKeyInput changed: {value}");
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

        // 장비 슬롯
        InitEquipItem();

        // NavMesh Agent
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = Speed;
        _agent.acceleration = 999;
        _agent.angularSpeed = 720;
        _agent.stoppingDistance = 0.1f;
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

        //if (ObjectType == Define.Object.OtherPlayer)
        //{
        //    float dist = Vector3.Distance(transform.position, _serverPos);
        //    if (dist > _minDist)
        //        _agent.Warp(_serverPos);
        //    else
        //        transform.position = Vector3.Lerp(transform.position, _serverPos, Time.deltaTime * _syncSpeed);
        //}
    }

    protected virtual void CheckUpdatedFlag() { }

    public override void OnDamaged()
    {
        Debug.Log("Player HIT !");
    }

    public void OnStop(S_Stop packet)
    {
        _agent.isStopped = true;
        _agent.ResetPath();
    }

    public void OnRespawn(S_Respawn packet)
    {
        _agent.Warp(new Vector3(packet.PosInfo.PosX, packet.PosInfo.PosY, packet.PosInfo.PosZ));
        transform.position = packet.PosInfo.ToVector();
        Hp = packet.Hp;
    }

    public void ChangeState(S_PlayerState packet)
    {
        Debug.Log($"Cur : {State}, Next : {packet.State}");
        State = packet.State;
    }

    #region Util
    protected string GetCharacterName()
    {
        return System.Enum.GetName(typeof(CharacterType), ObjInfo.Player.CharType);
    }
    #endregion

    #region Skill
    public override void UseSkill(S_Skill skillPacket)
    {
        // 서버에서 스킬 사용을 허락받으면
        if (skillPacket.CanUse)
        {
            //IsKeyInput = true;
            //State = CreatureState.Skill;

            //KeyCode keyCode =
            //    (skillPacket.SkillInfo.Amplification) ? (KeyCode)skillPacket.SkillInfo.AmplifiKeyCode : (KeyCode)skillPacket.SkillInfo.KeyCode;

            //ExecuteSkill(keyCode);

            //if (skillPacket.ObjectId == Managers.Object.MyPlayer.Id && !skillPacket.SkillInfo.Amplification)
            //    Managers.Object.MyPlayer.OnSkillConfirmed(skillPacket);

            //if (!_isSkillDebug)
            //    return;

            //Vector3 mousePos = new Vector3(skillPacket.MousePosX, 0, skillPacket.MousePosZ);
            //bool bProjectile = (DataManager.SkillDict[ObjInfo.Player.CharType][keyCode].type == "Projectile");
            //if (skillPacket.SkillInfo.Amplification && bProjectile)
            //    ChangeInfoSkillMesh(keyCode);
            //else
            //    CreateSkillMesh(keyCode, skillPacket.ChargeRatio, mousePos, bProjectile);
        }
    }

    //protected void ExecuteSkill(KeyCode keyCode)
    //{
    //    switch (keyCode)
    //    {
    //        case KeyCode.Q:
    //            Skill_Q();
    //            break;
    //        case KeyCode.W:
    //            Skill_W();
    //            break;
    //        case KeyCode.E:
    //            Skill_E();
    //            break;
    //        case KeyCode.R:
    //            Skill_R();
    //            break;
    //        case KeyCode.F:
    //            PassiveSkill();
    //            break;
    //    }
    //}

    // TODO : 이름 바꾸기?
    protected virtual void Skill_Q() { }

    protected virtual void Skill_W() { }

    protected virtual void Skill_E() { }

    protected virtual void Skill_R() { }
    protected virtual void PassiveSkill() { }
    public virtual void OnAttackTiming() { }

    public virtual void OnSkillMeshTiming(KeyCode key) { }

    IEnumerator CoStartSkill()
    {
        // 대기 시간
        IsKeyInput = true;
        State = CreatureState.Skill;
        yield return new WaitForSeconds(0.1f);
        AnimatorClipInfo[] clipInfos = _animator.GetCurrentAnimatorClipInfo(0);
        float length = 0;
        if (clipInfos.Length > 0)
        {
            length = clipInfos[0].clip.length / _animator.speed;
            Debug.Log($"Clip Name: {clipInfos[0].clip.name}, Length: {length}");
        }
        yield return new WaitForSeconds(length - 0.1f);
        Debug.Log("스킬 코루틴 종료");

        // TODO : TEMP
        CheckUpdatedFlag();
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
        _animator.CrossFadeInFixedTime(animInfo.Name, animInfo.Ratio);
    }
    #endregion

    Dictionary<KeyCode, SkillMesh> msDict = new Dictionary<KeyCode, SkillMesh>();

    #region SkillMesh
    public void ChangeInfoSkillMesh(KeyCode keyCode, float offset = 1.0f)
    {
        SkillMesh currentSkillMesh = msDict[keyCode];

        SkillHitbox hitbox = DataManager.SkillHitboxDict[ObjInfo.Player.CharType][keyCode];

        if (System.Enum.TryParse<SkillShape>(currentSkillMesh._hitbox.Shape, out SkillShape shape))
            currentSkillMesh.Draw(shape);
    }

    public virtual void CreateSkillMesh(KeyCode keyCode, float chargeRatio, Vector3 mousePos = new Vector3(), bool bProjectile = false)
    {
        SkillHitbox hitbox = DataManager.SkillHitboxDict[ObjInfo.Player.CharType][keyCode];
        if (hitbox.EndFrame <= 0)
            return;

        GameObject go = null;
        if (bProjectile) 
        {
            go = _projectile.gameObject;
            go.SetActive(true);
        }
        else
            go = gameObject;

        GameObject skillMeshGO = Managers.Resource.Instantiate("Debug/SkillMesh", go.transform);
        SkillMesh sm = skillMeshGO.GetComponent<SkillMesh>();
        if (sm == null) return;

        if (!msDict.ContainsKey(keyCode))  msDict.Add(keyCode, sm);
        else msDict[keyCode] = sm;

        if (false == hitbox.Charge)
            chargeRatio = 1;

        sm.Init(hitbox, go.transform, ObjInfo.Player.Team, chargeRatio, mousePos);     
    }

    #endregion

    #region NameTagAndHp
    protected void InitNameTag()
    {
        GameObject go = Managers.Resource.Instantiate("UI/SubItem/PlayerNameTagCanvas", gameObject.transform);
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
    public virtual void PlayEffectFromServer(EffectInfo fxInfo)
    {
        PlayEffectTransform(CreatureState.Skill, (KeyCode)fxInfo.KeyCode);
    }

    // 현재 상태, 키, 타겟팅 상대에게 이펙트
    protected virtual List<GameObject> PlayEffectTransform(CreatureState state, KeyCode key, EffectType type = EffectType.Caster,
       GameObject target = null, Transform targetTransform = null)
    {
        List<EffectData> effectList = Managers.Data.GetSkillEffectList(ObjInfo.Player.CharType, state, key, type);
        List<GameObject> EffectList = null;
        EffectList = Managers.FX.PlayEffect(ObjInfo.ObjectId, effectList, transform);

        return EffectList;
    }
    #endregion

    #region State:Dead
    //public virtual void OnRespawn(S_Respawn respawnPacket)
    //{
    //    //Hp = respawnPacket.Hp;
    //    //Stamina = respawnPacket.Stamina;
    //}
    #endregion

    public void SyncPosFromServer(S_Move movePacket)
    {
        _agent.isStopped = false;

        _serverPos = new Vector3
        {
            x = movePacket.PosInfo.PosX,
            y = movePacket.PosInfo.PosY,
            z = movePacket.PosInfo.PosZ
        };

        transform.rotation = movePacket.RotInfo;
    }
}
