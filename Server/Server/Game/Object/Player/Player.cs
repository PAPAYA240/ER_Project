using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using ServerCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using static ISkill;
using static Server.Data.DataUtils;
using static Server.Game.GameRoom;

namespace Server.Game
{
    public partial class Player : Creature
    {
        #region Player Info
        public ClientSession Session { get; set; }

        // Skill
        protected Dictionary<KeyCode, Skill> _skills = new Dictionary<KeyCode, Skill>();  // key : KeyCode
        Dictionary<KeyCode, CoolTime> _coolDownDict = new Dictionary<KeyCode, CoolTime>();
        class CoolTime
        {
            public bool isCoolDown;     // 쿨타임이 돌고 있는지 (false : 사용 가능)
            public float coolTime;      // 남은 쿨타임
        }

        // temp 임시 코드 나중에 삭제
        bool _isDeath = false;
        public bool IsDeath
        {
            get { return _isDeath; }
            set { _isDeath = value; }
        }

        // Inventory
        Dictionary<EquipItemType, EquipItemInfo> _equipItemSlot = new Dictionary<EquipItemType, EquipItemInfo>();
        ItemStat _totalItemStat = new ItemStat();
        List<ItemInfoBase> _inventory = new List<ItemInfoBase>();
        static int MaxInventorySlot = 10;

        #region Stat Property
        public override float Attack
        {
            get { return base.Attack + _totalItemStat.AttackDamage + _totalItemStat.AttackDamagePerLevel * Stat.Level + AdaptiveStat; }
            set { base.Attack = value; }
        }

        public override float Defense
        {
            get { return base.Defense + _totalItemStat.Defense; }
            set { base.Defense = value; }
        }

        public override float Speed 
        {
            get { return (base.Speed + _totalItemStat.FixedSpeed) * (1 + _totalItemStat.PercentageSpeed); }
            set { base.Speed = value; }
        }

        public override float MaxHp 
        {
            get { return base.MaxHp + _totalItemStat.MaxHp + _totalItemStat.MaxHpPerLevel * Stat.Level; }
            set { base.MaxHp = value; }
        }

        public override float Hp
        {
            get { return base.Hp; }
            set { Stat.Hp = Math.Clamp(value, 0, MaxHp); }
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

        #region KDA
        //KDA
        public int KillAmount {  get; set; }
        public int DeathAmount { get; set; }
        public int AsistAmount { get; set; }

        private int AsistTime = 15;

        private Dictionary<int, DamageRecord> _damageRecords = new Dictionary<int, DamageRecord>();

        public class DamageRecord
        { 
            public int Id;
            public float Damage;
            public TimeSpan TimeStamp;

            public DamageRecord(int id, float damage, TimeSpan timeStamp)
            {
                Id = id;
                Damage = damage;
                TimeStamp = timeStamp;
            }
        }

        private void UpdateDamageRecords()
        {
            if (null == Room) return;

            foreach (var record in _damageRecords.Values)
            {
                TimeSpan damageTime = Room.TimeStamp - record.TimeStamp;

                if(damageTime.TotalSeconds > 15)
                {
                    _damageRecords.Remove(record.Id);
                }
            }
        }

        public override void OnDamaged(GameObject attacker, float damage, bool isTrueDamage = false)
        {
            if (Room == null || State == CreatureState.Dead)
                return;

            UpdateDamageRecords();

            // 죽기 전에 추가하려고 순서를 이렇게 함.
            if (_damageRecords.TryGetValue(attacker.Id, out DamageRecord damageRecord)) // 이미 해당 플레이어에게 데미지를 입었다면 시간을 최신화.
            {
                damageRecord.Damage += damage;
                damageRecord.TimeStamp = Room.TimeStamp;
            }
            else
            {
                _damageRecords.Add(attacker.Id, new DamageRecord(attacker.Id, damage, Room.TimeStamp)); // 피해를 입은 적이 없다면 새로 추가.
            }

            base.OnDamaged(attacker, damage, isTrueDamage);
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
        }

        public override void Update()
        {
            if (IsDeath == true)
            {
                _isDeath = false;
                _stateMachine.ChangeState(new Player_DeadState(), this);
            }

            //base.Update();
            
            TickTokens(); // 토큰 만료/갱신
            _stateMachine.Update(this);
            CheckUpdateStat();
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
            // 용수야 도와줘

            if (Room == null)
                return;

            PosInfo.State = CreatureState.Dead;
            
            // KDA 변화 패킷
            S_ChangeKDA KdaPacket = new S_ChangeKDA();

            // 데스 증가
            {
                ++DeathAmount;
                KdaPacket.KDAs.Add(new KDAInfo { ObjectId = Id, Kill = KillAmount, Death = DeathAmount, Asist = AsistAmount });
            }
            
            // 킬 증가
            if(attacker is Player attackPlayer)
            {
                ++attackPlayer.KillAmount;
                KdaPacket.KDAs.Add(new KDAInfo { ObjectId = attackPlayer.Id, Kill = attackPlayer.KillAmount, Death = attackPlayer.DeathAmount, Asist = attackPlayer.AsistAmount });
            }

            // 어시 증가
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
        #endregion

        #region Skill
        private void MakeSkillDict()
        {
            // 본인 캐릭터의 스킬 정보만 추출
            Dictionary<KeyCode, SkillData> skills = DataManager.SkillDict[Info.Player.CharType];
            foreach (var skillData in skills)
            {
                Skill skill = new Skill();
                skill.SkillData = skillData.Value;

                _skills.Add(skillData.Key, skill);
            }

            _skills[KeyCode.T].CurLevel = 1;
            _skills[KeyCode.F].CurLevel = 1;
        }

        private void MakeCoolDownDict()
        {
            foreach (var skill in _skills)
            {
                _coolDownDict[skill.Key] = new CoolTime { isCoolDown = false, coolTime = 0.0f };
            }
        }

        private void MakeDict()
        {
            MakeSkillDict();
            MakeCoolDownDict();
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
                _inventory.Add(null); //비어 있는 인벤토리를 생성
            }
        }

        // 아이템 획득 함수
        public bool AcquireItem(ItemInfoBase item)
        {
            // _inventory 리스트에서 null 값이 있는 첫 번째 인덱스를 반환해줌.
            int firstEmptySlotIndex = _inventory.IndexOf(null);

            switch (item)
            {
                case ConsumableItemInfo consumableItem:
                    {
                        S_ChangeInventory packet = new S_ChangeInventory();

                        GameRoom room = Room;
                        ClientSession session = Session;

                        // 이미 있는 소모품이라면
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

                        // 새로운 소모품인데 인벤토리가 꽉 참.
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
                        // 인벤토리가 꽉 참.
                        if (firstEmptySlotIndex == -1)
                            return false;

                        _inventory[firstEmptySlotIndex] = equipItem;

                        // 획득한 장비의 칸이 비어 있으면 바로 장착 또는 얻은 장비의 등급이 높으면 자동 교체
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

        // 아이템 사용 함수(장착, 설치), 몇 번째 인벤에 있는 아이템을 사용하겠다.
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

        // 장비 아이템 장착 함수
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
            // 해당 페이즈에 장착할 아이템 세트의 아이디 리스트를 가져옴.
            List<int> itemIdList = DataManager.ItemSetDict[type][phase];

            foreach (int itemId in itemIdList)
            {
                EquipItemInfo item = DataManager.ItemDict[itemId] as EquipItemInfo;

                _equipItemSlot[item.Type] = item;

                S_ChangeEquipItem changeEquipItemPacket = new S_ChangeEquipItem();
                changeEquipItemPacket.ObjectId = Id;
                changeEquipItemPacket.ItemId = itemId;

                // 이미 푸쉬되어서 온 상황. 푸쉬된 함수안에 있거나 이 함수를 푸쉬해서 사용.
                GameRoom room = Room;
                room.Broadcast(changeEquipItemPacket);
            }

            UpdateItemStat();

            Console.WriteLine($"{Info.Player.CharType} Eqiup done!");
        }

        // 아이템 버리는 함수
        public void DiscardItem()
        {

        }

        // 인벤토리 내의 아이템을 아이디로 찾는 함수
        public ItemInfoBase FindItemInInventory()
        {


            return null;
        }

        // 인벤토리 스왑(아이템 위치 바꾸기) 
        public void SwapInventory(int firstIndex, int secondIndex)
        {
            // 1. 유효성 검사 (인덱스 범위 및 동일 인덱스 스왑 방지)
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
            else //빈칸 처리
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
            else //빈칸 처리
            {
                packet.Changes.Add(new ChangeInventoryInfo { ItemId = 0, InventoryIndex = secondIndex });

            }

            GameRoom room = Room;
            ClientSession session = Session;

            if( room != null && session != null)
                room.Push(session.Send, packet);
        }

        // 업데이트 아이템 스탯
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

                GameRoom room = Room;

                if (room != null)
                    //room.Push(room.Broadcast, packet);
                    room.Broadcast(packet);
            }
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
        #endregion

        #region Packet
        public void SendVisibleObjsPkt(List<int> Ids)
        {
            S_VisibleObjects visibleObjsPkt = new S_VisibleObjects();
            visibleObjsPkt.ObjectId = Id;
            visibleObjsPkt.VisibleObjectIds.AddRange(Ids);
            Session.Send(visibleObjsPkt);
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

        public void SendStopPacket(StopReason reason)
        {
            S_Stop packet = new S_Stop()
            {
                Id = Id,
                Reason = reason,
            };
            Room.Push(Room.Broadcast, packet);
        }

        public void SendAnimPacket(string animName, float ratio)
        {
            S_Anim packet = new S_Anim()
            { 
                ObjectId = Id,
                AnimInfo = new AnimInfo()
                {
                    Name = animName,
                    Ratio = ratio
                }
            };
            Room.Push(Room.Broadcast, packet);
        }

        public void SendMovePacket(PositionInfo posInfo, RotationInfo rotInfo)
        {
            S_Move packet = new S_Move()
            {
                ObjectId = Id,
                PosInfo = posInfo,
                RotInfo = rotInfo
            };

            Room.Push(Room.Broadcast, packet);

            //Console.WriteLine($"Char : {Info.Player.CharType} / x : {posInfo.PosX}, z : {posInfo.PosZ}");
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

        public void SendSkillMotion(SkillMotionType type, Vector3 start, Vector3 end,
                            float duration = 0f, string anim = default, string curveId = default,
                            bool serverCollision = false, bool authoritativeEnd = true)
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
                AuthoritativeEnd = authoritativeEnd
            };
            Room.Broadcast(pkt);
        }

        public void SendSkillConfirmPacket(bool canUse, KeyCode keyCode = KeyCode.None, VariantKey variants = default)
        {
            S_SkillConfirm packet = new S_SkillConfirm
            {
                ObjectId = Id,
                CanUse = canUse,
                SkillKey = (int)keyCode,
                Variants = variants,
                CostInfo = new CostInfo { CoolTime = GetCoolTime(keyCode), Stamina = Stamina },
                //InstanceId = ,
                //TargetId = , 
            };
            Room.Push(Room.Broadcast, packet);
        }

        public void SendDeadPacket(S_Respawn packet)
        {
            Room.Push(Room.Broadcast, packet);
        }

        public void SendMoveSyncPacket(PositionInfo targetPos, float speed = 1.0f)
        {
            S_MoveSync packet = new S_MoveSync
            {
                ObjectId = Id,
                TargetPos = targetPos,
                Speed = speed,
            };
            Room.Push(Room.Broadcast, packet);
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
                //Console.WriteLine($"coeff: {b.coeff}");
                //Console.WriteLine($"SkillAmplification: {SkillAmplification}");
                //Console.WriteLine($"b.targetCnt: {b.targetCnt}");
                //Console.WriteLine($"ratio: {ratio}");
            }
                
            Barrier = barrier;

            S_ChangeHp changePacket = new S_ChangeHp();
            changePacket.ObjectId = Id;
            changePacket.Hp = Hp;
            changePacket.Barrier = Barrier;
            //Console.WriteLine($"Barrier: {barrier}");
            Room.Push(Room.Broadcast, changePacket);
        }
        #endregion
    }
}
