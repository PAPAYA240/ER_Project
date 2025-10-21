using System;
using System.Collections;
using System.Collections.Generic;
using Data;
using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;
using static System.Runtime.CompilerServices.RuntimeHelpers;
using static Data.SkillEffectList;
using static UnityEngine.Rendering.DebugUI;

public class PlayerController : CreatureController
{
    bool _isKeyInput = false;
    int _atkCount = 1;
    int _maxAtkCount = 2;

    // NameTag
    protected UI_PlayerNameTag _nameTag;

    // 장착 아이템
    Dictionary<EquipItemType, EquipItemInfo> _equipItemSlot = new Dictionary<EquipItemType, EquipItemInfo>();
    public ItemStat ItemStat { get; private set; }
    protected GameObject _eqipWeapon = null;

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

        ObjectType = Define.Object.OtherPlayer;
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
    }

    protected virtual void CheckUpdatedFlag() { }

    public override void OnDamaged()
    {
        Debug.Log("Player HIT !");
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
        Debug.Log("스킬 패킷 받기");

        // 서버에서 스킬 사용을 허락받으면
        if (skillPacket.CanUse)
        {
            if(skillPacket.SkillInfo.Amplification)
                State = CreatureState.Skill;
            State = CreatureState.Skill;

            KeyCode keyCode =
                (skillPacket.SkillInfo.Amplification)? (KeyCode)skillPacket.SkillInfo.AmplifiKeyCode : (KeyCode)skillPacket.SkillInfo.KeyCode;

            ExecuteSkill(keyCode);

            if (Define.Object.MyPlayer == ObjectType && !skillPacket.SkillInfo.Amplification)
            {
                Managers.Object.MyPlayer.OnSkillConfirmed(skillPacket);
            }

            //StartCoroutine(CoStartSkill());
            //Debug.Log("스킬 코루틴 시작");

            //Vector3 MousePos = new Vector3();
            //Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            //if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
            //    MousePos = new Vector3(hit.point.x, hit.point.y, hit.point.z);

            //bool bProjectile = (DataManager.SkillDict[ObjInfo.Player.CharType][keyCode].type == "Projectile");
            //if (skillPacket.SkillInfo.Amplification && bProjectile)
            //    ChangeInfoSkillMesh(keyCode);
            //else
            //    CreateSkillMesh(keyCode, skillPacket.ChargeRatio, MousePos, bProjectile);
        }
    }

    protected void ExecuteSkill(KeyCode keyCode)
    {
        switch (keyCode)
        {
            case KeyCode.Q:
                Skill_Q();
                break;
            case KeyCode.W:
                Skill_W();
                break;
            case KeyCode.E:
                Skill_E();
                break;
            case KeyCode.R:
                Skill_R();
                break;
            case KeyCode.F:
                PassiveSkill();
                break;
        }
    }

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
    }

    public virtual void EquipItem(int itemId)
    {
        //TODO 아이템 도감에서 아이템을 가져와서 처리(+UI도)
        
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
    public virtual void OnRespawn(S_Respawn respawnPacket)
    {
        State = CreatureState.Idle;
        Hp = respawnPacket.Hp;
        Stamina = respawnPacket.Stamina;
    }
    #endregion


    public void SpawnProjectile()
    {
        _projectile.SetActive(true);

        Projectile projectileScript = _projectile.GetComponent<Projectile>();

        if (_equipTransform != null && projectileScript != null)
            projectileScript.Run(_equipTransform.position, transform.forward);
    }

    public void LaunchProjectile(List<CreatureController> target)
    {
        //_currentTarget = target;
        //// 스피드 감소, 시야 제공, 공격 시 => [스킬 피해 추가, 속박]
        //// 30/60/90/120/150(+스킬 증폭의 65%)
        //if (targetCreature != null)
        //    StartCoroutine(AbilitySkillE(targetCreature));

        // 공격을 받으면 데미지를 입혀야 함
    }

    public virtual void OnSkillAnimationEnd() { }
}
