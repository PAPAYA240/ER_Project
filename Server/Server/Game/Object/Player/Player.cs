using Google.Protobuf.Protocol;
using Server.Data;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using static Server.Data.DataUtils;

namespace Server.Game
{
    public partial class Player : Creature
    {
        #region Player Info
        public ClientSession Session { get; set; }

        // Skill
        public SkillController Skill { get; private set; }

        protected Dictionary<KeyCode, Skill> _skills = new Dictionary<KeyCode, Skill>();  // key : KeyCode

        public float ChargingRatio { get; set; } = 0; // How Long Charge 0 ~ 1

        // temp �ӽ� �ڵ� ���߿� ����
        bool _isDeath = false;
        public bool IsDeath
        {
            get { return _isDeath; }
            set { _isDeath = value; }
        }

        // Inventory
        Dictionary<EquipItemType, EquipItemInfo> _equipItemSlot = new Dictionary<EquipItemType, EquipItemInfo>();
        ItemStat _totalItemStat = new ItemStat();
        public ItemStat TotalItemStat { get { return _totalItemStat; } }
        List<ItemInfoBase> _inventory = new List<ItemInfoBase>();
        static int MaxInventorySlot = 10;

        #region Stat Property
        public override float Attack
        {
            get { return ComposeFinal(STAT_ATTACK, Stat.Attack + _totalItemStat.AttackDamage + _totalItemStat.AttackDamagePerLevel * Stat.Level + AdaptiveStat) ; }
            set { base.Attack = value; }
        }

        public override float Defense
        {
            get { return ComposeFinal(STAT_DEFENSE, Stat.Defense + _totalItemStat.Defense); }
            set { base.Defense = value; }
        }

        public override float Speed 
        {
            get { return ComposeFinal(STAT_MOVE_SPEED, Stat.MoveSpeed + _totalItemStat.FixedSpeed * (1 + _totalItemStat.PercentageSpeed), ignoreDebuff: IsCcImmune) ; }
            set { base.Speed = value; }
        }

        public override float AttackSpeed
        {
            get { return ComposeFinal(STAT_ATTACK_SPEED, (Stat.AttackSpeed + DataManager.WeaponDict[Info.Player.Weapon].AttackSpeed) * (1 + _totalItemStat.AttackSpeed), false, _mulBuffOffset); }
            set { base.AttackSpeed = value; }
        }

        public override float Healing
        {
            get { return ComposeFinal(STAT_HEALING, Stat.Healing); }
            set { base.Healing = value; }
        }

        public override float MaxHp 
        {
            get { return base.MaxHp + _totalItemStat.MaxHp + _totalItemStat.MaxHpPerLevel * Stat.Level; }
            set { base.MaxHp = value; }
        }

        public override float Hp
        {
            get { return base.Hp; }
            set 
            {
                float cur = Stat.Hp;

                if (value > cur) // 회복일 때만  Healing에 치유 증가/감소 반영
                {
                    float healAmount = (value - cur) * Healing; 
                    Stat.Hp = Math.Clamp(cur + healAmount, 0, MaxHp);
                }
                else // 데미지일 때는 그대로
                {
                    Stat.Hp = Math.Clamp(value, 0, MaxHp);
                }
            }
        }

        public override float HpRegen
        {
            get { return base.HpRegen * (1 + _totalItemStat.HpRegen); }
            set { Stat.HpRegen = Math.Max(value, 0); }
        }

        public override float MaxStamina 
        { 
            get { return base.MaxStamina + _totalItemStat.MaxStamina; }
            set { base.MaxStamina = value; }
        }

        public override float Stamina
        {
            get { return base.Stamina; }
            set { Stat.Stamina = Math.Clamp(value, 0, MaxStamina); }
        }

        public override float StaminaRegen
        {
            get { return base.StaminaRegen * (1 + _totalItemStat.StaminaRegen); }
            set { Stat.StaminaRegen = Math.Max(value, 0); } 
        }

        public float SkillAmplification
        {
            get { return (_totalItemStat.FixedSkillAmplification + _totalItemStat.SkillAmplificationPerLevel * Stat.Level + AdaptiveStat) 
                    * (1 + _totalItemStat.PercentageSkillAmplification); }
        }
        public override float FixedDefensePenetration { get { return _totalItemStat.FixedDefensePenetration; } }
        public override float PercentageDefensePenetration { get { return _totalItemStat.PercentageDefensePenetration; } }

        public float AdaptiveStat 
        { 
            get 
            {
                if (_totalItemStat.AdaptiveStat == 0)
                    return 0;

                float att, skillamp;
                att = _totalItemStat.AttackDamage + _totalItemStat.AttackDamagePerLevel * Stat.Level;
                skillamp = (_totalItemStat.FixedSkillAmplification + _totalItemStat.SkillAmplificationPerLevel * Stat.Level)
                    * (1 + _totalItemStat.PercentageSkillAmplification);

                if (att * 2 > skillamp)
                    return _totalItemStat.AdaptiveStat;
                else
                    return _totalItemStat.AdaptiveStat * 2; 
            } 
        }

        #endregion

        // StateMachine
        private PlayerStateMachine _stateMachine;
        private IPlayerState _currentState;
        public IPlayerState CurrentState
        {
            get { return _currentState; }
            set { _currentState = value; }
        }

        public IPlayerState ReservedState { get; set; }
        public Beacon Beacon { get; set; }

        // StatRegenerator
        public bool _isUpdatedStat = false;
        private StatRegenerator _statRegenerator;
        public override CreatureState State
        {
            get { return PosInfo.State; }
            set 
            {
                if (PosInfo.State == value)
                    return;

                PosInfo.State = value;
            }
        }

        #region CombatState
        // CombatState
        // 전투 시간 (용수야 여기야)
        private float _combatTime = 0f;
        private readonly float _nonCombatTime = 5f;
        public float CombatTime
        {
            get { return _combatTime; }
            set { _combatTime = value; }
        }

        private CombatState _curCombat;
        public CombatState CombatState
        {
            get { return _curCombat; }
            set { _curCombat = value; }
        }
        #endregion

        #region Yuki Privacy
        // 유키 단추용
        private static readonly int MaxStud = 4;
        private int _yukiStud_cnt = 4;

        public int YukiStud
        {
            get { return _yukiStud_cnt; }
            set { _yukiStud_cnt = value; }
        }

        // 유키 강화 평타용
        private float _attactActiveTime = 0f;
        private bool _isAttackActive = false;
        public bool AttackActive
        {
            get { return _isAttackActive; }
            set { _isAttackActive = value; }
        }
        #endregion

        #region Rozzi Privacy
        int _times = 0;
        public int Times
        {
            get { return _times; }
            set { _times = value; }
        }

        float _mulBuffOffset = 0f;
        public void AttackSpeedBuff(float ratio, int times)
        {
            _mulBuffOffset = ratio;
            Times = times;
            UpdateStatusFlag();
        }

        public bool OnAttackPerformed()
        {
            if (Times == 0)
            {
                _mulBuffOffset = 0f;
                UpdateStatusFlag();
                return false;
            }
            else
            {
                Times--;
                return true;
            }
        }
        #endregion

        #region KDA
        //KDA
        public int KillAmount {  get; set; }
        public int DeathAmount { get; set; }
        public int AsistAmount { get; set; }

        private const int AsistTimeMs = 15 * 1000; // 15초 → 밀리초로 저장

        private Dictionary<int, DamageRecord> _damageRecords = new Dictionary<int, DamageRecord>();

        public class DamageRecord
        { 
            public int Id;
            public float Damage;
            public long Tick;   // TimeSpan 대신 long(밀리초 tick)

            public DamageRecord(int id, float damage, long tick)
            {
                Id = id;
                Damage = damage;
                Tick = tick;
            }
        }

        private void UpdateDamageRecords()
        {
            if (null == Room) return;

            long now = Room.CurTick;

            // 지워야 하는 요소 수집
            List<int> toRemove = new List<int>();

            foreach (var record in _damageRecords.Values)
            {
                long delta = unchecked(now - record.Tick);

                if (delta > AsistTimeMs)
                    toRemove.Add(record.Id);
            }

            // 실제 삭제
            foreach (int id in toRemove)
                _damageRecords.Remove(id);
        }

        public int GetLastAttackerId()
        {
            int result = 0;
            long lastTick = 0;

            foreach(var recordKVP in _damageRecords)
            {
                if(lastTick < recordKVP.Value.Tick)
                {
                    lastTick = recordKVP.Value.Tick;
                    result = recordKVP.Key;
                }
            }

            return result;
        }

        public override void OnDamaged(GameObject attacker, float damage, bool isTrueDamage = false, bool isBasicAttack = false)
        {
            if (Room == null || State == CreatureState.Dead)
                return;

            UpdateDamageRecords();

            long now = Room.CurTick;

            if (_damageRecords.TryGetValue(attacker.Id, out DamageRecord damageRecord))
            {
                damageRecord.Damage += damage;
                damageRecord.Tick = now;  // 마지막 데미지 시각 갱신
            }
            else
            {
                _damageRecords.Add(attacker.Id, new DamageRecord(attacker.Id, damage, now));
            }

            base.OnDamaged(attacker, damage, isTrueDamage, isBasicAttack);
        }
        #endregion

        public Player()
        {
            ObjectType = GameObjectType.Player;

            _statRegenerator = new StatRegenerator(this, intervalMs: 1000);
            _statRegenerator.AddEffect(new BaseRegenEffect());
            _statRegenerator.AddEffect(new RestRegenEffect());

            _stateMachine = new PlayerStateMachine();           
        }

        public void Init()
        {
            StartRegen();
            _stateMachine.ChangeState(new Player_IdleState(), this);
            MakeDict();
            InitAboutItem();

            var cd = new CooldownController_Tick(this);
            Skill = new SkillController(this, cd);
        }

        public override void Update()
        {
            if (IsDeath == true)
            {
                _isDeath = false;
                _stateMachine.ChangeState(new Player_DeadState(), this);
            }

            // 일정 시간 지나면 비전투 (용수야 여기야)
            if (CombatState == CombatState.Combat)
            {
                _combatTime += TimeUtil.Instance.DeltaTime;
                if (_combatTime > _nonCombatTime)
                {
                    _combatTime = 0;

                    Console.WriteLine($"비전투 상태");
                    CombatState = CombatState.NonCombat;
                    S_CombatMode combatModePkt = new S_CombatMode();
                    combatModePkt.ObjectId = Id;
                    combatModePkt.CombatMode = CombatState;
                    Room.Broadcast(combatModePkt);

                    // 유키 단추용
                    if (Info.Player.CharType == CharacterType.Yuki)
                        YukiStud = MaxStud;
                }
            }

            // Player Death
            if (Hp <= 0 && State != CreatureState.Dead)
                ChangeState(new Player_DeadState());

            // 유키 강화 평타용
            if (AttackActive == true)
            {
                _attactActiveTime += TimeUtil.Instance.DeltaTime;

                if (_attactActiveTime > _nonCombatTime)
                {
                    AttackActive = false;
                }
            }

            UpdateAttackRange();

            //base.Update();

            TickTokens(); // ��ū ����/����
            _stateMachine.Update(this);
            _statRegenerator.Update();
            CheckUpdateStat();
            CheckUpdateStatus();
        }

        public void InitAboutItem()
        {
            MakeItemSlot();
            MakeInventory();
        }
        #endregion

        #region State : Dead
        public void OnDestroy()
        {
            StopRegen();
        }

        public override void OnDead(GameObject attacker)
        {
            if (Room == null)
                return;
            
            // KDA 패킷
            S_ChangeKDA KdaPacket = new S_ChangeKDA();

            // 데스 처리
            {
                ++DeathAmount;
                KdaPacket.KDAs.Add(new KDAInfo { ObjectId = Id, Kill = KillAmount, Death = DeathAmount, Asist = AsistAmount });
            }
            
            // 킬 처리
            if(attacker is Player attackPlayer)
            {
                ++attackPlayer.KillAmount;
                KdaPacket.KDAs.Add(new KDAInfo { ObjectId = attackPlayer.Id, Kill = attackPlayer.KillAmount, Death = attackPlayer.DeathAmount, Asist = attackPlayer.AsistAmount });
            }

            // 어시 처리
            {
                foreach(DamageRecord record in _damageRecords.Values)
                {
                    if (record.Id == attacker.Id)
                        continue;

                    Player asistPlayer = Room.FindPlayer(player => { return player.Id == record.Id; });
                    if(asistPlayer != null)
                    {
                        ++asistPlayer.AsistAmount;
                        KdaPacket.KDAs.Add(new KDAInfo { ObjectId = asistPlayer.Id, Kill = asistPlayer.KillAmount, Death = asistPlayer.DeathAmount, Asist = asistPlayer.AsistAmount });
                    }
                }
            }

            Room.Broadcast(KdaPacket);
        }
        #endregion

        #region State

        public void ChangeState(IPlayerState newState)
        {
            _stateMachine.ChangeState(newState, this);
        }

        public bool CanMove()
        {
            if(State == CreatureState.Stun || State == CreatureState.Dead)
                return false;

            return true;
        }
        #endregion

        #region Stat
        public void StartRegen() => _statRegenerator.Start();
        public void StopRegen() => _statRegenerator.Stop();

        public bool CanRegenerate()
        {
            if (State == CreatureState.Dead)
                return false;

            if (Hp == MaxHp && Stamina == MaxStamina)
                return false;

            return true;
        }

        public void UseHealPack(float amount, float durationSeconds)
        {
            _statRegenerator.AddEffect(new HealPackEffect(amount, durationSeconds));
        }

        private void CheckUpdateStat()
        {
            if (_isUpdatedStat)
            {
                S_ChangeStat statPacket = new S_ChangeStat();
                statPacket.ObjectId = Id;
                statPacket.Hp = Hp;
                statPacket.Barrier = Barrier;
                statPacket.Stamina = Stamina;

                GameRoom room = Room;
                if(room != null)
                    room.Push(room.Broadcast, statPacket);

                _isUpdatedStat = false;
            }
        }

        void UpdateAttackRange()
        {
            float prevAttackRange = AttackRange;

            switch (Info.Player.CharType)
            {
                case CharacterType.Yuki:
                    // Q 활성화 되어있으면 BonusAttackRange = 0.25f
                    break;
                case CharacterType.Abigail:
                    if (Skill.IsPassiveAttackReady())
                        BonusAttackRange = 0.1f;
                    else
                        BonusAttackRange = 0f;
                    break;
            }

            if (Math.Abs(prevAttackRange - AttackRange) > 0.0001f)
                SendChangeAttackRangePacket();
        }
        #endregion

        #region Skill
        private void MakeSkillDict()
        {
            // ���� ĳ������ ��ų ������ ����
            Dictionary<KeyCode, SkillData> skills = DataManager.SkillDict[Info.Player.CharType];
            foreach (var skillData in skills)
            {
                Skill skill = new Skill();
                skill.SkillData = skillData.Value;

                _skills.Add(skillData.Key, skill);
            }

            _skills[KeyCode.T].CurLevel = 1;
            _skills[KeyCode.F].CurLevel = 1;

            if (_skills.TryGetValue(KeyCode.D, out var value))
                value.CurLevel = 1;
        }

        private void MakeDict()
        {
            MakeSkillDict();
        }

        public bool SkillLevelUp(KeyCode key)
        {
            bool result = false;

            switch (key)
            {
                case KeyCode.Q:
                case KeyCode.W:
                case KeyCode.E:
                    {
                        if (_skills[key].CurLevel == 0 && Info.StatInfo.Level >= 1)
                        {
                            _skills[key].CurLevel++;
                            result = true;
                        }
                        else if (_skills[key].CurLevel == 1 && Info.StatInfo.Level >= 3)
                        {
                            _skills[key].CurLevel++;
                            result = true;
                        }
                        else if (_skills[key].CurLevel == 2 && Info.StatInfo.Level >= 5)
                        {
                            _skills[key].CurLevel++;
                            result = true;
                        }
                        else if (_skills[key].CurLevel == 3 && Info.StatInfo.Level >= 7)
                        {
                            _skills[key].CurLevel++;
                            result = true;
                        }
                        else if (_skills[key].CurLevel == 4 && Info.StatInfo.Level >= 9)
                        {
                            _skills[key].CurLevel++;
                            result = true;
                        }
                    }
                    break;
                case KeyCode.R:
                    {
                        if (_skills[key].CurLevel == 0 && Info.StatInfo.Level >= 6)
                        {
                            _skills[key].CurLevel++;
                            result = true;
                        }
                        else if (_skills[key].CurLevel == 1 && Info.StatInfo.Level >= 11)
                        {
                            _skills[key].CurLevel++;
                            result = true;
                        }
                        else if (_skills[key].CurLevel == 2 && Info.StatInfo.Level >= 16)
                        {
                            _skills[key].CurLevel++;
                            result = true;
                        }
                    }
                    break;
                case KeyCode.T:
                    {
                        if (_skills[key].CurLevel == 0 && Info.StatInfo.Level >= 1)
                        {
                            _skills[key].CurLevel++;
                            result = true;
                        }
                        else if (_skills[key].CurLevel == 1 && Info.StatInfo.Level >= 5)
                        {
                            _skills[key].CurLevel++;
                            result = true;
                        }
                        else if (_skills[key].CurLevel == 2 && Info.StatInfo.Level >= 9)
                        {
                            _skills[key].CurLevel++;
                            result = true;
                        }
                    }
                    break;
            }

            if (Info.Player.CharType == CharacterType.Abigail && key == KeyCode.Q && result)
            {
                _skills[KeyCode.F1].CurLevel++;
            }

            return result;
        }

        public Skill GetSkill(KeyCode keyCode)
        {
            return _skills[keyCode];
        }

        public int GetSkillLevel(KeyCode keyCode)
        {
            return _skills[keyCode].CurLevel;
        }

        #endregion

        #region Item
        private void MakeItemSlot()
        {
            for (int i = 0; i < (int)EquipItemType.End; ++i)
            {
                _equipItemSlot.Add((EquipItemType)i, new EquipItemInfo());
            }
        }

        private void MakeInventory()
        {
            for (int i = 0; i < MaxInventorySlot; ++i)
            {
                _inventory.Add(null); //��� �ִ� �κ��丮�� ����
            }
        }

        // ������ ȹ�� �Լ�
        public bool AcquireItem(ItemInfoBase item)
        {
            // _inventory ����Ʈ���� null ���� �ִ� ù ��° �ε����� ��ȯ����.
            int firstEmptySlotIndex = _inventory.IndexOf(null);

            switch (item)
            {
                case ConsumableItemInfo consumableItem:
                    {
                        S_ChangeInventory packet = new S_ChangeInventory();

                        GameRoom room = Room;
                        ClientSession session = Session;

                        // �̹� �ִ� �Ҹ�ǰ�̶��
                        for (int i = 0; i < MaxInventorySlot; ++i)
                        {
                            if (_inventory[i] != null && _inventory[i] is ConsumableItemInfo itemInfo && _inventory[i].Id == consumableItem.Id)
                            {
                                itemInfo.Count += consumableItem.Count;

                                packet.Changes.Add(new ChangeInventoryInfo { ItemId = itemInfo.Id, InventoryIndex = i, Count = itemInfo.Count });

                                if (room != null && session != null)
                                    room.Push(session.Send, packet);

                                return true;
                            }
                        }

                        // ���ο� �Ҹ�ǰ�ε� �κ��丮�� �� ��.
                        if (firstEmptySlotIndex == -1)
                            return false;

                        _inventory[firstEmptySlotIndex] = consumableItem;

                        packet.Changes.Add(new ChangeInventoryInfo { ItemId = consumableItem.Id, InventoryIndex = firstEmptySlotIndex, Count = consumableItem.Count });

                        if (room != null && session != null)
                            room.Push(session.Send, packet);

                        return true; 
                    }
                case EquipItemInfo equipItem:
                    {
                        // �κ��丮�� �� ��.
                        if (firstEmptySlotIndex == -1)
                            return false;

                        _inventory[firstEmptySlotIndex] = equipItem;

                        // ȹ���� ����� ĭ�� ��� ������ �ٷ� ���� �Ǵ� ���� ����� ����� ������ �ڵ� ��ü
                        if (_equipItemSlot[equipItem.Type] == null || _equipItemSlot[equipItem.Type].Grade < equipItem.Grade)
                            EquipItem(equipItem, firstEmptySlotIndex);
                        else
                        {
                            S_ChangeInventory packet = new S_ChangeInventory();
                            packet.Changes.Add(new ChangeInventoryInfo { ItemId = equipItem.Id, InventoryIndex = firstEmptySlotIndex });

                            GameRoom room = Room;
                            ClientSession session = Session;

                            if (room != null && session != null)
                                room.Push(session.Send, packet);
                        }

                        return true;
                    }
                default:
                    break;
            }

            return false;
        }

        // ������ ��� �Լ�(����, ��ġ), �� ��° �κ��� �ִ� �������� ����ϰڴ�.
        public void UseItem(int index)
        {
            if (null == _inventory[index])
                return;

            switch (_inventory[index])
            {
                case ConsumableItemInfo consumableItem:
                    {
                        consumableItem.Use();
                        consumableItem.Count--;
                        if (consumableItem.Count == 0)
                            _inventory[index] = null;
                    }
                    break;
                case EquipItemInfo equipItem:
                    EquipItem(equipItem, index);
                    break;
            }
        }

        // ��� ������ ���� �Լ�
        private void EquipItem(EquipItemInfo item, int inventoryIndex)
        {
            S_ChangeInventory changeInventoryPacket = new S_ChangeInventory();
            S_ChangeEquipItem changeEquipItemPacket = new S_ChangeEquipItem();

            if (null == _equipItemSlot[item.Type])
            {
                _equipItemSlot[item.Type] = item;
                _inventory[inventoryIndex] = null;

                changeInventoryPacket.Changes.Add(new ChangeInventoryInfo { ItemId = 0, InventoryIndex = inventoryIndex });
            }
            else
            {
                EquipItemInfo temp = _equipItemSlot[item.Type];
                _equipItemSlot[item.Type] = item;
                _inventory[inventoryIndex] = temp;

                changeInventoryPacket.Changes.Add(new ChangeInventoryInfo { ItemId = _inventory[inventoryIndex].Id, InventoryIndex = inventoryIndex });
            }

            changeEquipItemPacket.ObjectId = Id;
            changeEquipItemPacket.ItemId = item.Id;

            GameRoom room = Room;
            ClientSession session = Session;

            if (room != null)
            {
                room.Push(session.Send, changeInventoryPacket);
                room.Push(room.Broadcast, changeEquipItemPacket);
            }

            UpdateItemStat();
        }

        public void EquipItemSet(CharacterType type, int phase)
        {
            if (!DataManager.ItemSetDict.ContainsKey(type))
                return;

            List<int> itemIdList = DataManager.ItemSetDict[type][phase];

            foreach (int itemId in itemIdList)
            {
                EquipItemInfo item = DataManager.ItemDict[itemId] as EquipItemInfo;

                _equipItemSlot[item.Type] = item;

                S_ChangeEquipItem changeEquipItemPacket = new S_ChangeEquipItem();
                changeEquipItemPacket.ObjectId = Id;
                changeEquipItemPacket.ItemId = itemId;

                // �̹� Ǫ���Ǿ �� ��Ȳ. Ǫ���� �Լ��ȿ� �ְų� �� �Լ��� Ǫ���ؼ� ���.
                GameRoom room = Room;
                room.Broadcast(changeEquipItemPacket);
            }

            UpdateItemStat();

            Console.WriteLine($"{Info.Player.CharType} Eqiup done!");
        }

        // ������ ������ �Լ�
        public void DiscardItem()
        {

        }

        // �κ��丮 ���� �������� ���̵�� ã�� �Լ�
        public ItemInfoBase FindItemInInventory()
        {


            return null;
        }

        // �κ��丮 ����(������ ��ġ �ٲٱ�) 
        public void SwapInventory(int firstIndex, int secondIndex)
        {
            // 1. ��ȿ�� �˻� (�ε��� ���� �� ���� �ε��� ���� ����)
            if (firstIndex < 0 || firstIndex >= _inventory.Count ||
                secondIndex < 0 || secondIndex >= _inventory.Count ||
                firstIndex == secondIndex)
                return; 

            ItemInfoBase temp = _inventory[firstIndex];
            _inventory[firstIndex] = _inventory[secondIndex];
            _inventory[secondIndex] = temp;

            S_ChangeInventory packet = new S_ChangeInventory();

            if(_inventory[firstIndex] != null)
            {
                if (_inventory[firstIndex] is ConsumableItemInfo firstItem)
                {
                    packet.Changes.Add(new ChangeInventoryInfo { ItemId = _inventory[firstIndex].Id, InventoryIndex = firstIndex, Count = firstItem.Count });
                }
                else
                {
                    packet.Changes.Add(new ChangeInventoryInfo { ItemId = _inventory[firstIndex].Id, InventoryIndex = firstIndex });
                }
            }
            else //��ĭ ó��
            {
                packet.Changes.Add(new ChangeInventoryInfo { ItemId = 0, InventoryIndex = firstIndex });
            }

            if( _inventory[secondIndex] != null )
            {
                if (_inventory[secondIndex] is ConsumableItemInfo secondItem)
                {
                    packet.Changes.Add(new ChangeInventoryInfo { ItemId = _inventory[secondIndex].Id, InventoryIndex = secondIndex, Count = secondItem.Count });
                }
                else
                {
                    packet.Changes.Add(new ChangeInventoryInfo { ItemId = _inventory[secondIndex].Id, InventoryIndex = secondIndex });
                }
            }
            else //��ĭ ó��
            {
                packet.Changes.Add(new ChangeInventoryInfo { ItemId = 0, InventoryIndex = secondIndex });

            }

            GameRoom room = Room;
            ClientSession session = Session;

            if( room != null && session != null)
                room.Push(session.Send, packet);
        }

        // ������Ʈ ������ ����
        private void UpdateItemStat()
        {
            lock (this)
            {
                _totalItemStat = new ItemStat();

                foreach (var itemKvp in _equipItemSlot)
                {
                    if (itemKvp.Value == null)
                        continue;

                    _totalItemStat += itemKvp.Value.ItemStat;
                }

                S_ChangeItemStat packet = new S_ChangeItemStat();
                packet.ObjectId = Id;
                packet.ItemStat = _totalItemStat;

                Hp += _totalItemStat.MaxHp + _totalItemStat.MaxHpPerLevel * Stat.Level;
                Stamina += _totalItemStat.MaxStamina;
                _isUpdatedStat = true;

                UpdateStatusFlag();

                GameRoom room = Room;

                if (room != null)
                    //room.Push(room.Broadcast, packet);
                    room.Broadcast(packet);
            }
        }

        public void SendItemStat()
        {
            S_ChangeItemStat packet = new S_ChangeItemStat();
            packet.ObjectId = Id;
            packet.ItemStat = _totalItemStat;

            GameRoom room = Room;

            if (room != null)
                room.Broadcast(packet);
        }

        #endregion

        #region Level
        public int CheckLevelUp()
        {
            int levelUp = 0;
            while (DataManager.ExpDict.ContainsKey(Stat.Level) &&
                Stat.Exp >= DataManager.ExpDict[Stat.Level])
            {
                Stat.Exp -= DataManager.ExpDict[Stat.Level];
                Stat.Level++;
                StatInfo statInfo = DataManager.StatGrowthDict[Info.Player.CharType];
                Stat.AddStat(statInfo);
                levelUp++;
            }

            return levelUp;
        }
        #endregion

        #region Util
        public bool CanAttack()
        {
            if (State == CreatureState.Dead)
                return false;

            return true;
        }

        public GameObject FindTarget(int targetId)
        {
            return ObjectManager.Instance.Find(targetId);
        }

        public void LookAtMouse(Vector2 mousePos, bool sendPacket = true)
        {
            Vector2 myPos = new Vector2(Info.PosInfo.PosX, Info.PosInfo.PosZ);
            Vector2 dir = mousePos - myPos;

            if (dir.LengthSquared() < 0.0001f)
                return;

            float angle = (float)Math.Atan2(dir.X, dir.Y);
            Quaternion rot = Quaternion.CreateFromAxisAngle(Vector3.UnitY, angle);

            RotInfo = new RotationInfo
            {
                Qx = rot.X,
                Qy = rot.Y,
                Qz = rot.Z,
                Qw = rot.W
            };

            if(sendPacket)
                SendChangeTransformPacket();
        }

        #endregion

        #region Packet
        public void SendVisibleObjsPkt(List<int> Ids)
        {
            S_VisibleObjects visibleObjsPkt = new S_VisibleObjects();
            visibleObjsPkt.ObjectId = Id;
            visibleObjsPkt.VisibleObjectIds.AddRange(Ids);
            Room.Push(Session.Send, visibleObjsPkt);
        }

        public void SendStatePacket()
        {
            S_PlayerState packet = new S_PlayerState()
            {
                ObjectId = Id,
                State = State,
            };
            Room.Push(Room.Broadcast, packet);
        }

        public void SendStopPacket(StopReason reason = StopReason.StopAll)
        {
            S_Stop packet = new S_Stop()
            {
                Id = Id,
                Reason = reason,
            };
            Room.Push(Room.Broadcast, packet);
        }

        public void SendAnimPacket(string animName, float ratio = 0.05f, float speed = 0, bool isChangeSpeed = false)
        {
            S_Anim packet = new S_Anim()
            { 
                ObjectId = Id,
                AnimInfo = new AnimInfo()
                {
                    Name = animName,
                    Ratio = ratio,
                    Speed = speed,
                    IsChangeSpeed = isChangeSpeed
                }
            };
            Room.Push(Room.Broadcast, packet);
        }

        public void SendSetMoveTarget(bool isGround, int targetId, PositionInfo posOpt = null)
        {
            S_SetMoveTarget packet = new S_SetMoveTarget
            {
                Id = Id,
                IsGround = isGround,
                TargetId = isGround ? 0 : targetId,
                TargetPos = isGround && posOpt != null ? new PositionInfo(posOpt) : null
            };
            Room.Push(Room.Broadcast, packet);
        }

        public void SendSkillMotion(SkillMotionType type, Vector3 start, Vector3 end, bool authoritativeEnd = false,
                            float duration = 0f, string anim = default, string curveId = default,
                            bool serverCollision = false, bool canFloat = false)
        {
            S_SkillMotion pkt = new S_SkillMotion
            {
                ObjectId = Id,
                Type = type,
                StartX = start.X,
                StartY = start.Y,
                StartZ = start.Z,
                EndX = end.X,
                EndY = end.Y,
                EndZ = end.Z,
                Duration = duration,
                Anim = anim ?? "",
                CurveId = curveId ?? "",
                ServerCollision = serverCollision,
                AuthoritativeEnd = authoritativeEnd,
            };
            Room.Push(Room.Broadcast, pkt);
        }

        public void SendSkillEffect(
            Vector2 mousePos,
            KeyCode keyCode = KeyCode.None, 
            bool sendLookatMousePacket = false,
            Vector3 targetPos = new Vector3(),
            Quaternion targetRot = default(Quaternion),
            string type = "Caster", 
            string name = "")
        {
            Vector2 myPos = new Vector2(Info.PosInfo.PosX, Info.PosInfo.PosZ);
            Vector2 dir = mousePos - myPos;
            if (dir.LengthSquared() < 0.0001f)
                return;

            float angle = (float)Math.Atan2(dir.Y, dir.X);
            Quaternion rot = Quaternion.CreateFromAxisAngle(Vector3.UnitY, angle);

            RotationInfo newRot = new RotationInfo
            {
                Qx = rot.X,
                Qy = rot.Y,
                Qz = rot.Z,
                Qw = rot.W
            };
            RotInfo = newRot;

            S_Fx fxPacket = new S_Fx
            {
                ObjectId = Id,
                CanLookatMouse = sendLookatMousePacket,
                SkillKey = (int)keyCode,
                MousePosX = mousePos.X,
                MousePosZ = mousePos.Y,
                TargetPosition = new PositionInfo { PosX = targetPos.X, PosY = targetPos.Y, PosZ = targetPos.Z },
                TargetRotation = new RotationInfo { Qx = targetRot.X, Qy = targetRot.Y, Qz = targetRot.Z, Qw = targetRot.W },
                Type = type,
                FxName = name
            };

            Room.Push(Room.Broadcast, fxPacket);
        }

        public void SendSkillConfirmPacket
            (bool canUse, 
            KeyCode keyCode = KeyCode.None, 
            bool canMoveDuringCast = false, 
            bool sendCostPacket = true)
        {
            S_SkillConfirm packet;

            if (canUse)
            {
                packet = new S_SkillConfirm
                {
                    ObjectId = Id,
                    CanUse = canUse,
                    SkillKey = (int)keyCode,
                    CanMove = canMoveDuringCast,
                };
               
                if(sendCostPacket)
                    SendSkillCostPacket(keyCode);
            }
            else
            {
                packet = new S_SkillConfirm
                {
                    ObjectId = Id,
                    CanUse = canUse,
                    SkillKey = (int)keyCode,
                };
            }

            Room.Push(Session.Send, packet);
        }

        public void SendFxPacket()
        {

        }

        public void SendDeadPacket(S_Respawn packet)
        {
            Room.Push(Room.Broadcast, packet);
        }

        public void SendRestPacket(S_Rest packet)
        {
            Room.Push(Room.Broadcast, packet);
        }

        public void SendSkillCollisionRequestPacket(KeyCode keyCode, int requestId, CollisionType type, Vector3 startPos, Vector3 endPos)
        {
            S_SkillCollisionRequest packet = new S_SkillCollisionRequest
            {
                SkillKey = (int)keyCode,
                RequestId = requestId,
                Type = type,
                StartX = startPos.X,
                StartZ = startPos.Z,
                EndX = endPos.X,
                EndZ = endPos.Z
            };
            Room.Push(Session.Send, packet);
        }

        public void SendSkillCostPacket(KeyCode keyCode, float coolTime)
        {
            S_SkillCost costPacket = new S_SkillCost
            {
                ObjectId = Id,
                SkillKey = (int)keyCode,
                CostInfo = new CostInfo { CoolTime = coolTime, Stamina = Stamina }
            };

            Room.Push(Session.Send, costPacket);
        }

        public void SendSkillCostPacket(KeyCode keyCode)
        {
            CommitSkillUsage(keyCode);

            S_SkillCost costPacket = new S_SkillCost
            {
                ObjectId = Id,
                SkillKey = (int)keyCode,
                CostInfo = new CostInfo { CoolTime = GetCoolTime(keyCode), Stamina = Stamina }
            };

            Room.Push(Session.Send, costPacket);
        }

        public void SendTargetChangePacket(S_TargetChange packet)
        {
            Room.Push(Session.Send, packet);
        }

        public void SendMoveSyncPacket(PositionInfo targetPos)
        {
            S_MoveSync packet = new S_MoveSync
            {
                ObjectId = Id,
                TargetPos = targetPos,
            };
            Room.Push(Session.Send, packet);
        }

        public void SendChangeTransformPacket(bool isWarp = false) // 수동으로 플레이어 위치or회전 수정한 후에 보내는 패킷
        {
            S_ChangeTransform pkt = new S_ChangeTransform
            {
                ObjectId = Id,
                PosInfo = this.PosInfo.Clone(),
                RotInfo = new RotationInfo(RotInfo),
                IsWarp = isWarp
            };

            Room.Push(Room.Broadcast, pkt);
        }

        public void SendCanStopSkillPacket(bool canStopSkill)
        {
            S_CanStopSkill pkt = new S_CanStopSkill
            {
                ObjectId = Id,
                CanStopSkill = canStopSkill,
            };
            Room.Push(Room.Broadcast, pkt);
        }

        public void SendChangeAttackRangePacket()
        {
            S_ChangeAttackRange changeAtkRangePkt = new S_ChangeAttackRange();
            changeAtkRangePkt.ObjectId = Id;
            changeAtkRangePkt.AttackRange = AttackRange;
            Room.Push(Session.Send, changeAtkRangePkt);
        }

        public void SendUntargetablePacket(bool IsUntargetable)
        {
            S_Untargetable untargetablePkt = new S_Untargetable();
            untargetablePkt.ObjectId = Id;
            untargetablePkt.Untargetable = IsUntargetable;
            Room.Push(Room.Broadcast, untargetablePkt);
        }

        public void SendUnstoppablePacket(bool IsUnstoppable)
        {
            S_Unstoppable unstoppablePkt = new S_Unstoppable();
            unstoppablePkt.ObjectId = Id;
            unstoppablePkt.Unstoppable = IsUnstoppable;
            Room.Push(Room.Broadcast, unstoppablePkt);
        }

        public void SendYukiSkillEffect(Vector2 mousePos, bool sendPacket = true)
        {
            Vector2 myPos = new Vector2(Info.PosInfo.PosX, Info.PosInfo.PosZ);
            Vector2 dir = mousePos - myPos;

            if (dir.LengthSquared() < 0.0001f)
                return;

            float angle = (float)Math.Atan2(dir.X, dir.Y);
            Quaternion rot = Quaternion.CreateFromAxisAngle(Vector3.UnitY, angle);

            RotInfo = new RotationInfo
            {
                Qx = rot.X,
                Qy = rot.Y,
                Qz = rot.Z,
                Qw = rot.W
            };

            S_YukiSkillEffect pkt = new S_YukiSkillEffect
            {
                ObjectId = Id,
                PosInfo = this.PosInfo.Clone(),
                RotInfo = new RotationInfo(RotInfo)
            };

            Room.Push(Room.Broadcast, pkt);
        }
        #endregion

        #region StatusEffect(버프, 디버프), Barrier(방어막) 관련
        public override void UpdateBarrier()
        {
            float barrier = 0;

            foreach (var b in _barriers)
            {
                float ratio = Math.Min(b.ratioPerTarget * b.targetCnt, b.maxRatio);
                barrier += (b.value + (b.coeff * SkillAmplification * 0.01f)) * (1f + ratio * 0.01f);
            }
                
            Barrier = barrier;

            S_ChangeHp changePacket = new S_ChangeHp();
            changePacket.ObjectId = Id;
            changePacket.Hp = Hp;
            changePacket.Barrier = Barrier;
            //Console.WriteLine($"Barrier: {barrier}");
            Room.Push(Room.Broadcast, changePacket);
        }

        public void CheckUpdateStatus()
        {
            if (_isUpdatedStatus)
            {
                S_ChangeStatus packet = new S_ChangeStatus()
                {
                    ObjectId = Id,

                    MoveSpeed = Speed,
                    Attack = Attack,
                    AttackSpeed = AttackSpeed,
                    Defense = Defense,
                    Healing = Healing,
                };

                Room.Push(Session.Send, packet);
                _isUpdatedStatus = false;
                Console.WriteLine($"AttackSpeed : {AttackSpeed}");
            }
        }

        public void SendUpdateStatusPacket(bool IsUnStoppable)
        {
            S_ChangeStatus packet = new S_ChangeStatus()
            {
                ObjectId = Id,

                MoveSpeed = Speed,
                Attack = Attack,
                //AttackSpeed = 
                Defense = Defense,
                Healing = Healing,
            };

            Room.Push(Session.Send, packet);
        }
        #endregion
    }
}
