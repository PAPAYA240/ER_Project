using Google.Protobuf.Protocol;
using Server.Data;
using ServerCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;
using static Server.Data.DataUtils;

namespace Server.Game
{
    public class Player_Prev : GameObject
    {
        public ClientSession Session { get; set; }

        private PlayerStateMachine _stateMachine;

        //public Player_Prev()
        //{
        //    ObjectType = GameObjectType.Player;

        //    _statRegenerator = new StatRegenerator(this);
        //    _statRegenerator.AddEffect(new BaseRegenEffect());
        //    _statRegenerator.AddEffect(new RestRegenEffect());

        //    _stateMachine = new PlayerStateMachine();
        //    _stateMachine.ChangeState(new Player_IdleState(), this);
        //}

        //public override void Update()
        //{
        //    //base.Update();
        //    _stateMachine.Update(this);
        //    CheckUpdateStat();
        //}

        //public void ChangeState(IPlayerState newState)
        //{
        //    _stateMachine.ChangeState(newState, this);
        //}

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        protected Dictionary<KeyCode, Skill> _skills = new Dictionary<KeyCode, Skill>();  // key : KeyCode
        Dictionary<KeyCode, CoolTime> _coolDownDict = new Dictionary<KeyCode, CoolTime>();
        Dictionary<EquipItemType, EquipItemInfo> _equipItemSlot = new Dictionary<EquipItemType, EquipItemInfo>();
        ItemStat _totalItemStat = new ItemStat();
        List<ItemInfoBase> _inventory = new List<ItemInfoBase>();

        #region Stat Property
        public override float Attack 
        {
            get { return base.Attack + _totalItemStat.AttackDamage + _totalItemStat.AttackDamagePerLevel * Stat.Level; }
            set { base.Attack = value; }
        }

        public override float Defense 
        {
            get { return base.Defense + _totalItemStat.Defense; }
            set { base.Defense = value; }
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

        public float SkillAmplification
        {
            get { return (_totalItemStat.FixedSkillAmplification + _totalItemStat.SkillAmplificationPerLevel * Stat.Level) 
                    * _totalItemStat.PercentageSkillAmplification; }
        }

        public override float FixedDefensePenetration { get { return _totalItemStat.FixedDefensePenetration; } }
        public override float PercentageDefensePenetration { get { return _totalItemStat.PercentageDefensePenetration; } }

        #endregion

        class CoolTime
        {
            public bool isCoolDown;     // 쿨타임이 돌고 있는지 (false : 사용 가능)
            public float coolTime;      // 남은 쿨타임
        }

        // StatRegenerator
        public bool _isUpdatedStat = false;
        private StatRegenerator _statRegenerator;

        //Inventory
        static int MaxInventorySlot = 10;

        

        public GameObject SkillTarget { get; set; }
        public KeyCode UsedTargetingSkill { get; set; }

        #region Init
        public void Init()
        {
            MakeDict();
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
            //base.Update();
            CheckUpdateStat();
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
                _ = CoRespawnTime(diePacket.RespawnTime, false);
                diePacket.RespawnTime = 0;
            }
            else
            {
                _ = CoRespawnTime(diePacket.RespawnTime);
                diePacket.RespawnTime = DataManager.RespawnDict[Stat.Level];
            }

            Room.Broadcast(diePacket);
        }
        #endregion

        #region Stat
        public void StartRegen() => _statRegenerator.Start();
        public void StopRegen() => _statRegenerator.Stop();

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
                Room.Push(Room.Broadcast, statPacket);

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
