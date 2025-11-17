using Data;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using System.Collections.Generic;
using UnityEngine;

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
    public bool CanStopSkill { get; set; } = false;

    float _lastOperateTime;
    readonly float _operateLockTime = 0.1f;

    public int CurPhase { get; set; } = 999;

    [Header("X-Ray Settings")]
    [SerializeField] private int playerWeaponStencilID = 100;
    [SerializeField] private bool disablePlayerWeaponXRay = true;

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

        if(ObjInfo.Player.CharType == CharacterType.Hyunwoo)
        {
            Destroy(_input);
            _input = gameObject.GetOrAddComponent<HyunwooInputController>();
        }

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

        InitializeXRay();
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
            _view.TargetId = atkCmd.TargetId;
            Managers.Network.Send(atkCmd);
        }
        else
        {
            var operate = _input.GetOperateCommand();
            if (operate != null)
            {
                _lastOperateTime = Time.time;
                Managers.Network.Send(operate);
            }
            else
            {
                if(Time.time - _lastOperateTime >= _operateLockTime) // operate 명령 후 0.1초 경과했을 경우
                {
                    // 3) 우클릭 유지: 타겟 이동 or 땅 이동
                    var setMove = _input.GetSetMoveTarget();
                    if (setMove != null)
                    {
                        _view.ApplyLocalSetMoveTarget(setMove);
                        Managers.Network.Send(setMove);
                    }
                }
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
        //var deathCmd = _input.GetDieCommand();
        //if (deathCmd != null)
        //    Managers.Network.Send(deathCmd);

        // temp 임시 코드 나중에 삭제
        var tempCmd = _input.Get_KeyInputForTestCommand();
        if (tempCmd != null)
            Managers.Network.Send(tempCmd);

        //if (_agent.hasPath)
        //{
        //    if (_agent.velocity.sqrMagnitude > 0.01f)
        //    {
        //        Quaternion targetRot = Quaternion.LookRotation(_agent.velocity.normalized);
        //        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 20f);
        //    }
        //}

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
    public void OnServerUpdate(S_SkillCollisionRequest packet) => _skill.OnSkillCollisionRequest(packet);
    public void OnServerUpdate(S_SkillCost packet) => _skill.OnSkillCost(packet);

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

    #region Shader
    void InitializeXRay()
    {
        if (disablePlayerWeaponXRay)
            SetupPlayerWeaponXRay();
    }

    void SetupPlayerWeaponXRay()
    {
        // Player 본체
        SetXRayGroup(gameObject, playerWeaponStencilID);

        // 현재 장착된 무기
        if (_eqipWeapon != null)
        {
            SetXRayGroup(_eqipWeapon, playerWeaponStencilID);
        }
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
        if (newWeapon != null && disablePlayerWeaponXRay)
        {
            SetXRayGroup(newWeapon, playerWeaponStencilID);
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

    void SetupRenderingLayer()
    {
        uint playerLayer = 1u << 1; // Layer 1

        SetRenderingLayerMask(gameObject, playerLayer);

        if (_eqipWeapon != null)
        {
            SetRenderingLayerMask(_eqipWeapon, playerLayer);
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

    
    #endregion

    #region Packet

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
