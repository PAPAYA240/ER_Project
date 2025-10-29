using Google.Protobuf.Protocol;
using Server.Data;
using ServerCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using static Server.Data.DataUtils;

namespace Server.Game
{
    public class Player : Creature
    {
        public ClientSession Session { get; set; }

        protected Dictionary<KeyCode, Skill> _skills = new Dictionary<KeyCode, Skill>();  // key : KeyCode
        Dictionary<KeyCode, CoolTime> _coolDownDict = new Dictionary<KeyCode, CoolTime>();
        Dictionary<EquipItemType, EquipItemInfo> _equipItemSlot = new Dictionary<EquipItemType, EquipItemInfo>();
        ItemStat _totalItemStat = new ItemStat();
        List<ItemInfoBase> _inventory = new List<ItemInfoBase>();


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

        class CoolTime
        {
            public bool isCoolDown;     // 쿨타임이 돌고 있는지 (false : 사용 가능)
            public float coolTime;      // 남은 쿨타임
        }

        // StatRegenerator
        public bool _isUpdatedStat = false;
        private StatRegenerator _statRegenerator;
        private long _lastUpdateTick;

        //Inventory
        static int MaxInventorySlot = 10;

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
        }

        public GameObject SkillTarget { get; set; }
        public KeyCode UsedTargetingSkill { get; set; }

        #region Init
        public void Init()
        {
            MakeDict();

            _lastUpdateTick = Environment.TickCount64;
            StartRegen();

            InitAboutItem();
        }

        public void OnDestroy()
        {
            StopRegen();
        }

        public void MakeDict()
        {
            MakeSkillDict();
            MakeCoolDownDict();
        }

        public void InitAboutItem()
        {
            MakeItemSlot();
            MakeInventory();
        }
        #endregion

        #region Update
        public override void Update()
        {
            UpdateStatRegenerator();
        }
        #endregion

        #region State : Dead
        public override void OnDead(GameObject attacker)
        {
            if (Room == null)
                return;

            PosInfo.State = CreatureState.Dead;

            S_Die diePacket = new S_Die();
            diePacket.ObjectId = Id;
            diePacket.AttackerId = attacker.Id;
            if(Stat.Level == 1)
            {
                diePacket.RespawnTime = 0;
                _ = CoRespawnTime(diePacket.RespawnTime, false);
            }
            else
            {
                diePacket.RespawnTime = DataManager.RespawnDict[Stat.Level];
                _ = CoRespawnTime(diePacket.RespawnTime);
            }

            Room.Broadcast(diePacket);
            
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

        #region Stat
        public void StartRegen() => _statRegenerator.Start();
        public void StopRegen() => _statRegenerator.Stop();

        private void UpdateStatRegenerator()
        {
            long now = Environment.TickCount64;
            int deltaMs = 0;
            long diff = now - _lastUpdateTick;
            if (diff < 0)
                diff = 0;
            if (diff > int.MaxValue)
                diff = int.MaxValue;
            deltaMs = (int)diff;
            _lastUpdateTick = now;

            _statRegenerator.Update(deltaMs);
            CheckUpdateStat();
        }

        public bool CanRegenerate()
        {
            if(State == CreatureState.Dead)
                return false;

            if(Hp == MaxHp && Stamina == MaxStamina)
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
        public bool CanUseSkill(KeyCode keyCode)
        {
            if (_skills[keyCode].CurLevel == 0)
                return false;

            // 쿨타임 체크
            if (!CheckCoolTime(keyCode))
                return false;

            // 스테미나 체크
            if (!CheckStamina(keyCode))
                return false;

            return true;
        }

        // 체크 끝나면 데이터 변경
        public void CommitSkillUsage(KeyCode keyCode)
        {
            // 쿨타임 재기 시작
            _ = CoInputCooltime(keyCode, FindSkill(keyCode).CurLevelCooldown);

            // 스테미나 감소
            Stamina -= FindSkill(keyCode).CurLevelStamina;
        }

        public float GetCoolTime(KeyCode key)
        {
            return _coolDownDict[key].coolTime;
        }

        private bool CheckCoolTime(KeyCode key)
        {
            if (!_coolDownDict[key].isCoolDown)
                return true;

            return false;
        }

        private bool CheckStamina(KeyCode key)
        {
            if(Stamina < FindSkill(key).CurLevelStamina)
                return false;

            return true;
        }

        private async Task CoInputCooltime(KeyCode key, float time)
        {
            _coolDownDict[key].isCoolDown = true;

            var sw = Stopwatch.StartNew();

            while (sw.Elapsed.TotalSeconds < time)
            {
                _coolDownDict[key].coolTime = (float)(time - sw.Elapsed.TotalSeconds);
                await Task.Delay(10); // 0.01초마다 남은 쿨타임 갱신
            }

            _coolDownDict[key].isCoolDown = false;
            _coolDownDict[key].coolTime = 0.0f;
        }

        private Skill FindSkill(KeyCode key)
        {
            return _skills[key];
        }

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
        }

        private void MakeCoolDownDict()
        {
            foreach (var skill in _skills)
            {
                _coolDownDict[skill.Key] = new CoolTime { isCoolDown = false, coolTime = 0.0f };
            }
        }



        //TODO D랑 F는 어떻게 하지?
        public bool SkillLevelUp(KeyCode key)
        {
            bool result = false;

            switch (key)
            {
                case KeyCode.Q:
                case KeyCode.W:
                case KeyCode.E:
                    {
                        if(_skills[key].CurLevel == 0 && Info.StatInfo.Level >= 1)
                        {
                            _skills[key].CurLevel++;
                            result = true;
                        }
                        else if(_skills[key].CurLevel == 1 && Info.StatInfo.Level >= 3)
                        {
                            _skills[key].CurLevel++;
                            result = true;
                        }
                        else if(_skills[key].CurLevel == 2 && Info.StatInfo.Level >= 5)
                        {
                            _skills[key].CurLevel++;
                            result = true;
                        }
                        else if(_skills[key].CurLevel == 3 && Info.StatInfo.Level >= 7)
                        {
                            _skills[key].CurLevel++;
                            result = true;
                        }
                        else if(_skills[key].CurLevel == 4 && Info.StatInfo.Level >= 9)
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

        public float GetSkillDamage(KeyCode keyCode)
        {
            return _skills[keyCode].GetSkillDamage();
        }

        public Skill GetSkill(KeyCode keyCode)
        {
            return _skills[keyCode];
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
            for(int i = 0; i < MaxInventorySlot; ++i)
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

        #region Respawn
        private async Task CoRespawnTime(float respawnTime, bool respawnAtZero = true)
        {
            var sw = Stopwatch.StartNew();

            while (sw.Elapsed.TotalSeconds < respawnTime)
            {
                await Task.Delay(10); // 0.01초마다 남은 쿨타임 갱신
            }

            if (Room == null)
                return;

            S_Respawn respawnPacket = new S_Respawn();
            respawnPacket.ObjectId = Id;
            if(true == respawnAtZero)
            {
                respawnPacket.PosInfo = Info.PosInfo = new PositionInfo
                {
                    PosX = 0,
                    PosY = 0,
                    PosZ = 0
                };
                respawnPacket.RotInfo = Info.RotInfo = new RotationInfo
                {
                    Qx = 0,
                    Qy = 0,
                    Qz = 0,
                    Qw = 1
                };
            }
            else
            {
                respawnPacket.PosInfo = Info.PosInfo;
                respawnPacket.RotInfo = Info.RotInfo;
            }

            respawnPacket.Hp = Hp = MaxHp;
            respawnPacket.Stamina = Stamina = MaxStamina;
            Session.Send(respawnPacket);

            State = CreatureState.Idle;
        }
        #endregion

        #region Packet
        public void SendSkillPkt()
        {

        }

        public void SendVisibleObjsPkt(List<int> Ids)
        {
            S_VisibleObjects visibleObjsPkt = new S_VisibleObjects();
            visibleObjsPkt.ObjectId = Id;
            visibleObjsPkt.VisibleObjectIds.AddRange(Ids);
            Session.Send(visibleObjsPkt);
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

    }
}
