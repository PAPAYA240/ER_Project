using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using ServerCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Numerics;
using System.Threading.Tasks;
using static ISkillHandler;
using static Server.Data.DataUtils;
using static Server.Game.GameRoom;

namespace Server.Game
{
    public partial class Player : Creature
    {
        public ClientSession Session { get; set; }

        // Skill
        public PlayerFlags Flags { get; } = new PlayerFlags();
        public class PlayerFlags
        {
            public bool IsInSkillMotion;
            public Vector3 SkillMotionStart;
            public Vector3 SkillMotionEnd;
            public float SkillMotionEndTimeUtc; // utcSeconds
        }

        public PendingSkillProposal PendingProposal;
        public struct PendingSkillProposal
        {
            public int SkillKey;
            public int Seq;
            public SkillCollisionProposal Prop;
            public bool Has;
        }

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

        public Player()
        {
            ObjectType = GameObjectType.Player;

            _statRegenerator = new StatRegenerator(this);
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

        public void OnDestroy()
        {
            StopRegen();
        }

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
                Room.Push(Room.Broadcast, statPacket);

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

        public void InitAboutItem()
        {
            MakeItemSlot();
            MakeInventory();
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
    }
}
