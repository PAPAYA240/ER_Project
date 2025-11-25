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

    public SoundController Sound;


    public SkillIndicator Indicator { get { return _skillIndicator; } }

    private SkillIndicator _skillIndicator;

    // Inventory
    List<ItemInfoBase> _inventory = new List<ItemInfoBase>();

    public WeaponInfo MyWeapon { get; set; } = new WeaponInfo();

    public float WeaponMasteryAS { get; set; }
    public float ItemAttackSpeed { get; set; } = 0;

    public bool CanStopSkill { get; set; } = false;

    float _lastAttackTime;
    readonly float _attackLockTime = 0.1f;

    float _lastOperateTime;
    readonly float _operateLockTime = 0.1f;

    public int CurPhase { get; set; } = 999;

 

    private void Awake()
    {
        _skill = gameObject.GetOrAddComponent<PlayerSkillController>();
        _input = gameObject.GetOrAddComponent<PlayerInputController>();
        _view = gameObject.GetOrAddComponent<PlayerViewController>();
        _UI = gameObject.GetOrAddComponent<PlayerUIController>();
        Sound = gameObject.GetComponent<SoundController>();
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

        // inven
        MakeInventory();
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
            _lastAttackTime = Time.time;
            _view.TargetId = atkCmd.TargetId;
            Managers.Network.Send(atkCmd);
        }
        else
        {
            if(Time.time - _lastAttackTime >= _attackLockTime)
            {
                var operate = _input.GetOperateCommand();
                if (operate != null)
                {
                    _lastOperateTime = Time.time;
                    Managers.Network.Send(operate);
                }
                else
                {
                    if (Time.time - _lastOperateTime >= _operateLockTime) // operate 명령 후 0.1초 경과했을 경우
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

        // 아이템 사용
        var useItemCmd = _input.GetUseItemCommand();
        if (useItemCmd != null)
            Managers.Network.Send(useItemCmd);

        // temp 임시 코드 나중에 삭제
        //var deathCmd = _input.GetDieCommand();
        //if (deathCmd != null)
        //    Managers.Network.Send(deathCmd);

        // temp 임시 코드 나중에 삭제
        var tempCmd = _input.Get_KeyInputForTestCommand();
        if (tempCmd != null)
            Managers.Network.Send(tempCmd);

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

    public override void OnDead()
    {
        base.OnDead();

        if (Sound != null)
            Sound.GetRandomVoice("Dead");
    }
    // 서버 응답 전달
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
                UI.PlayerInterface.SetInventoryItem(null, change.InventoryIndex);
            }
            else
            {
                if (DataManager.ItemDict.TryGetValue(change.ItemId, out ItemInfoBase item))
                {
                    if (change.Count == 0)
                    {
                        // 장비 아이템
                        _inventory[change.InventoryIndex] = item;
                        UI.PlayerInterface.SetInventoryItem(item, change.InventoryIndex);
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
                        UI.PlayerInterface.SetInventoryItem(consumableItem, change.InventoryIndex);
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

    private void MakeInventory()
    {
        for (int i = 0; i < 10; ++i)
        {
            _inventory.Add(null); //비어 있는 인벤토리를 생성
        }
    }

    public bool CheckInventory(int idx)
    {
        if(idx == 0)
        {
            if (_inventory[9] != null)
                return true;
        }
        else 
        {
            if (_inventory[idx - 1] != null)
                return true;
        }

        return false;
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
}
