using Data;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using static Data.SkillEffectList;
using static UI_PlayerInterface;
using static UI_SkillBase;
using static UnityEngine.GraphicsBuffer;

public class MyPlayerController : PlayerController
{
    private PlayerInputController _input;
    private PlayerViewController _view;
    public PlayerViewController View {  get { return _view; } }
    private PlayerSkillController _skill;
    private PlayerUIController _UI;
    public PlayerUIController UI {  get { return _UI; } }

    public SkillIndicator Indicator { get { return _skillIndicator; } }

    private SkillIndicator _skillIndicator;

    // Inventory
    List<ItemInfoBase> _inventory = new List<ItemInfoBase>();

    public WeaponInfo MyWeapon { get; set; } = new WeaponInfo();

    public float WeaponMasteryAS { get; set; }
    public float ItemAttackSpeed { get; set; } = 0;
    public float AttackSpeed
    {
        get
        {
            float baseSpeed = Stat.AttackSpeed + MyWeapon.AttackSpeed;
            float multiplier = 1 + WeaponMasteryAS + ItemAttackSpeed;
            return baseSpeed * multiplier;
        }
    }

    private void Awake()
    {
        _skill = gameObject.GetOrAddComponent<PlayerSkillController>();
        _input = gameObject.GetOrAddComponent<PlayerInputController>();
        _view = gameObject.GetOrAddComponent<PlayerViewController>();
        _UI = gameObject.GetOrAddComponent<PlayerUIController>();
    }

    protected override void Init()
    {
        base.Init();

        Camera.main.gameObject.GetOrAddComponent<CameraController>().SetPlayer(gameObject);
        _skill.Init();
        _UI.Init();

        _nameTag.GetComponentInChildren<UI_PlayerNameTag>().SetHPColor();

        // 스킬 인디케이터
        _skillIndicator = gameObject.GetOrAddComponent<SkillIndicator>();

        // 전장의 안개 카메라 설정
        GameObject fogCamGo = GameObject.Find("FogCamera");
        if (null != fogCamGo)
        {
            string fogLayerName = $"FogTeam{ObjInfo.Player.Team}";
            fogCamGo.GetComponent<Camera>().cullingMask |= (1 << LayerMask.NameToLayer(fogLayerName));
        }
    }

    private void Update()
    {
        // 1) 정지(S/H)
        var stopCmd = _input.GetStopCommand();
        if (stopCmd != null)
            Managers.Network.Send(stopCmd);

        // 2) 우클릭 : 타겟 공격 의도
        var atkCmd = _input.GetAttackCommand();
        if (atkCmd != null)
        {
            _view.RotateAttack(atkCmd);
            //Managers.Network.Send(atkCmd);
        }
        else
        {
            // 3) 우클릭 유지: 타겟 이동 or 땅 이동
            var setMove = _input.GetSetMoveTarget();
            if (setMove != null)
            {
                _view.ApplyLocalSetMoveTarget(setMove);
                Managers.Network.Send(setMove);
            }
        }

        // 스킬
        // 스킬 레벨 업
        var skillLevelUpCmd = _input.GetSkillLevelUpCommand();
        if (skillLevelUpCmd != KeyCode.None)
            _UI.TrySkillLevelUp(skillLevelUpCmd);
        else
        {
            var skillCmd = _input.GetSkillCommand();
            if (skillCmd != null)
                Managers.Network.Send(skillCmd);
        }

        // 휴식(X)
        var restCmd = _input.GetRestCommand();
        if (restCmd != null)
            Managers.Network.Send(restCmd);

        // temp 임시 코드 나중에 삭제
        var deathCmd = _input.GetDieCommand();
        if (deathCmd != null)
            Managers.Network.Send(deathCmd);

        CheckUpdatedFlag();
    }

    protected override void UpdateCharging()
    {
        if (_agent == null)
            return;

        //if (_agent.remainingDistance <= _agent.stoppingDistance)
        //{
        //    if (_moveKeyPressed)
        //        PlayAnimation("CHARGING", 0.1f);

        //    _agent.speed = _originSpeed;
        //    _moveKeyPressed = false;
        //}
        //UpdateTransform();
    }

    public bool RequiresCharge(KeyCode key)
    {
        return DataManager.SkillDict[ObjInfo.Player.CharType][key].canCharge;
    }

    // 서버 응답 전달
    //public void OnServerUpdate(S_Idle packet) => _view.OnIdle(packet);
    public void OnServerUpdate(S_Move packet) => _view.OnMove(packet);
    public void OnServerUpdate(S_MoveSync packet) => _view.OnMoveSync(packet);
    public void OnServerUpdate(S_Anim packet) => _view.OnAnim(packet);
    public void OnServerUpdate(S_ChangeHp packet) => _view.OnHpChanged(packet);
    public void OnServerUpdate(S_Die packet) => _view.OnDead(packet);
    public void OnServerUpdate(S_Respawn packet) => _view.OnRespawn(packet);
    public void OnServerUpdate(S_SetMoveTarget packet)
    {
        // 서버가 내려준 의도 그대로 로컬 네비 실행
        _view.ApplyLocalSetMoveTarget(new C_SetMoveTarget
        {
            IsGround = packet.IsGround,
            TargetId = packet.TargetId,
            TargetPos = packet.TargetPos != null ? new PositionInfo(packet.TargetPos) : null
        });
    }
    public void OnServerUpdate(S_Stop packet) => _view.OnStop(packet);
    public void OnServerUpdate(S_SkillMotion packet) => _skill.OnSkill(packet);
    public void OnServerUpdate(S_SkillConfirm packet) => _skill.OnSkillConfirm(packet);

    #region UI
    public override void SetKDA(int kill, int death, int asist)
    {
        base.SetKDA(kill, death, asist);
        UI.PlayerHUD.SetKDA(kill, death, asist);
    }

    public override void EquipItem(int itemId)
    {
        base.EquipItem(itemId);
        if (UI.PlayerInterface == null)
            return;
        UI.PlayerInterface.Equip(DataManager.ItemDict[itemId] as EquipItemInfo);
    }  
    #endregion

    #region Effect
    protected GameObject FindEffect(string fxName)
    {
        return Managers.FX.Effect.FindEffect(ObjInfo.ObjectId, fxName);
    }
    // 스킬 시전 이펙트 : TODO : 나중에 키에 따른 이펙트만 지워줄 것
    protected void RemoveAllEffect()
    {
        Managers.FX.RemoveAllEffect(ObjInfo.ObjectId);
    }
    protected void RemoveEffect(string fxName)
    {
        Managers.FX.Effect.RemoveEffect(ObjInfo.ObjectId, FindEffect(fxName));
    }
    protected List<GameObject> PlayEffect(string fxName, Vector3 position = new Vector3(), Quaternion rot = new Quaternion())
    {
        List<EffectData> effectList = Managers.Data.GetEffectsByPrefabName(fxName);
    
        return Managers.FX.PlayEffect(ObjInfo.ObjectId, effectList, transform, position, rot);
    }
    
    protected override List<GameObject> PlayEffectTransform(CreatureState state, KeyCode key, EffectType type = EffectType.Caster,
        GameObject target = null, Transform targetTransform = null)
    {
        List<EffectData> effectList =
            Managers.Data.GetSkillEffectList(ObjInfo.Player.CharType, state, key, type);
    
        List<GameObject> EffectList = null;
    
        // 타겟의 이펙트
        if (type == EffectType.HitTarget && target != null)
        {
            EffectList = (targetTransform != null) ?
             Managers.FX.PlayEffect(ObjInfo.ObjectId, effectList, targetTransform)
             : Managers.FX.PlayEffect(ObjInfo.ObjectId, effectList, target.transform);
        }
        // 나의 이펙트
        else if (type == EffectType.Caster)
        {
            EffectList = (targetTransform != null) ?
            Managers.FX.PlayEffect(ObjInfo.ObjectId, effectList, targetTransform)
            : Managers.FX.PlayEffect(ObjInfo.ObjectId, effectList, this.transform);
        }
    
        return EffectList;
    }
    
    protected List<GameObject> PlayEffectAtPosition(CreatureState state, KeyCode key, Vector3 position, Quaternion rot, EffectType type = EffectType.Caster)
    {
        List<EffectData> effectList = Managers.Data.GetSkillEffectList(ObjInfo.Player.CharType, state, key, type);
    
        if (effectList == null || effectList.Count == 0)
            return null;
    
        List<GameObject> EffectList = Managers.FX.PlayEffect(ObjInfo.ObjectId, effectList, this.transform, position, rot);
    
        return EffectList;
    }
    #endregion

    #region Inventory, EquipItem
    
    public void ChangeInventory(S_ChangeInventory packet)
    {
        foreach (var change in packet.Changes)
        {
            //빈칸 처리
            if (change.ItemId == 0)
            {
                //TODO UI 작업
                _inventory[change.InventoryIndex] = null;
            }
            else
            {
                if (DataManager.ItemDict.TryGetValue(change.ItemId, out ItemInfoBase item))
                {
                    if (change.Count == 0)
                    {
                        // 장비 아이템
                        _inventory[change.InventoryIndex] = item;
                    }
                    else
                    {
                        // 소모 아이템
                        ConsumableItemInfo consumableItem = item as ConsumableItemInfo;
                        if (consumableItem == null)
                        {
                            //Debug.Log($"Error. [{GetType()}] in ChangeInventory, consumableItem == null");
                            continue;
                        }
                        consumableItem.Count = change.Count;
    
                        _inventory[change.InventoryIndex] = consumableItem;
                    }
                }
                else
                {
                    //유효하지 않은 아이템 아이디.
                }
            }
        }
    }
    
    public override void UpdateItemStat(ItemStat stat)
    {
        base.UpdateItemStat(stat);
    
        // 스탯 UI 업데이트
    }
    #endregion

    #region Util
    public void UpdateTransform(bool isWarp = false)
    {
        CellPos = transform.position;
        RotInfo = transform.rotation;
        _updated = true;
        //_isWarp = isWarp;
    }

    protected Vector3 GetCursorPos()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            return new Vector3(hit.point.x, 0, hit.point.z); // 충돌 지점이 곧 월드 좌표
        }
        return new Vector3(-1, -1, -1);
    }
    #endregion

    #region Packet
    protected void SendFXPacket(KeyCode key)
    {
        //C_Fx fxPacket = new C_Fx();

        //fxPacket.FxInfo = new EffectInfo() { KeyCode = (int)_keyCode };

        //Managers.Network.Send(fxPacket);
    }

    protected override void CheckUpdatedFlag()
    {
        if (_updated)
        {
            C_MoveSync syncPacket = new C_MoveSync();
            syncPacket.PosInfo = PosInfo;
            syncPacket.RotInfo = RotInfo;
            Managers.Network.Send(syncPacket);
            _updated = false;
        }
    }

    public void SendPacket(IMessage packet)
    {
        Managers.Network.Send(packet);
    }

    #endregion

    protected override void UpdateHp() { base.UpdateHp(); _UI.UpdateHp(); }
    protected override void UpdateMaxHp() { base.UpdateMaxHp(); _UI.UpdateMaxHp(); } 
    protected override void UpdateStamina() { base.UpdateStamina(); _UI.UpdateStamina(); }
    protected override void UpdateMaxStamina() { base.UpdateMaxStamina(); _UI.UpdateMaxStamina(); }
    public void UpdateLevel() { _UI.UpdateLevel(); }
    public void UpdateCool() { _UI.UpdateCool(); }

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //protected KeyCode _keyCode = KeyCode.None;
    //protected bool _isUseSkill = false;
    //protected float _attackRange = 3.0f; // Temp
    //protected virtual void UpdateSkillKeyInput() { }
    //protected GameObject TryGetAttackableObject(float radius = 0.1f) { return null; }
    //protected int SkillTargetId { get; set; }
    //protected void SetSkillInput(KeyCode keyCode) { }
    //protected void SetMovementState() { }
    //protected void SendFXPacket(KeyCode key) { }
    //protected virtual void ResetCharacterState() { }
    //public virtual void OnSkillConfirmed(S_Skill skillPacket) { }
    //protected virtual void GetMouseInput(int mouseButton) { }
    //protected void ResetTarget() { }
    //protected void ResetCoroutine(Coroutine coroutine) { }
    //public HashSet<int> VisibleObjectIds { get; set; } = new HashSet<int>();
    //protected void LookAtTarget(Vector3 targetPos, bool snapToTarget = false, float speed = 20.0f) { }
    //protected void LookAtMouse() { }
    //protected Vector3 GetTargetPos(float range, bool isMaxDistance = true) { return Vector3.zero; }
    //protected Vector3 GetReachablePosition(Vector3 startPos, Vector3 targetPos, out NavMeshHit navHit) { navHit = new NavMeshHit();  return Vector3.zero;  }
    //protected Vector3 GetCursorPos() { return Vector3.zero; }
    //protected float GetCurrentAnimClipLength() { return 0f; }
    //public UI_PlayerInterface PlayerInterface { get; protected set; }
}
