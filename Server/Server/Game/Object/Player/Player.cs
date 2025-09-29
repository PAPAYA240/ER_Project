using Google.Protobuf;
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
    public class Player : GameObject
    {
        public ClientSession Session { get; set; }

        // StateMachine
        private PlayerStateMachine _stateMachine;

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
        ItemStat _totalItemStat = new ItemStat();

        public float SkillAmplification
        {
            get
            {
                return (_totalItemStat.FixedSkillAmplification + _totalItemStat.SkillAmplificationPerLevel * Stat.Level)
                    * _totalItemStat.PercentageSkillAmplification;
            }
        }

        public Skill GetSkill(KeyCode keyCode)
        {
            return _skills[keyCode];
        }

        #endregion

        #region Level
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

        #region Packet
        public void SendVisibleObjsPkt(List<int> Ids)
        {
            S_VisibleObjects visibleObjsPkt = new S_VisibleObjects();
            visibleObjsPkt.ObjectId = Id;
            visibleObjsPkt.VisibleObjectIds.AddRange(Ids);
            Session.Send(visibleObjsPkt);
        }

        public void SendStatePacket(IMessage packet)
        {
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
        #endregion
    }
}
