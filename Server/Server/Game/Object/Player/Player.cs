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
using static Server.Data.DataUtils;
using static Server.Game.GameRoom;

namespace Server.Game
{
    public partial class Player : Creature
    {
        public ClientSession Session { get; set; }

        public PlayerFlags Flags { get; } = new PlayerFlags();
        public class PlayerFlags
        {
            public bool IsInSkillMotion;
            public Vector3 SkillMotionStart;
            public Vector3 SkillMotionEnd;
            public float SkillMotionEndTimeUtc; // utcSeconds
        }

        #region Stat Property
        ItemStat _totalItemStat = new ItemStat();
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

                //S_PlayerState packet = new S_PlayerState();
                //packet.ObjectId = Id;
                //packet.State = value;
                //SendStatePacket(packet);

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
            //InitAboutItem();
        }

        public override void Update()
        {
            //base.Update();
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
        protected Dictionary<KeyCode, Skill> _skills = new Dictionary<KeyCode, Skill>();  // key : KeyCode
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
                            float duration, string anim, string curveId,
                            bool serverCollision, bool authoritativeEnd)
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
        #endregion
    }
}
