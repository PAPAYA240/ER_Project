using System;
using System.Collections.Generic;
using System.Numerics;
using Google.Protobuf.Protocol;
using Lucene.Net.Index;
using Server.Data;

namespace Server.Game
{
    public interface IMonsterState
    {
        void Enter(Monster monster);
        void Execute(Monster monster);
        void Exit(Monster monster);
        void OnHit(Monster monster, Creature target);
    }
    public interface ISkillBehavior
    {
        void OnStart(Monster caster, MonsterSkillData skillData);
        void OnUpdate(Monster caster);
        void OnHit(Monster caster, Creature target);
        void OnEnd(Monster caster);
    }

    public class Monster : Creature
    {
        #region Fields
        // State
        private IMonsterState _currentState;
        // Packet
        private int _sequenceId = 0;

        // Skills
        List<MonsterSkill> _skills = new List<MonsterSkill>();
        public MonsterSkill CurrentSkill { get; private set; }
        public float DelaySkillAnimationTimer = 0;

        // Position
        public Vector3 _spawnPosition = new Vector3();
        public Quaternion _spawnRotation = new Quaternion();

        public bool ReturnToSpawn { get; set; }
        public Vector3 _lastTargetPos = new Vector3();

        // Detection
        private const float ACTIVE_RANGE = 20f;

        // Events
        public Action<GameObject> OnAttacked;

        // ExpInfo
        const int DroneExp = 500;
        const int OmegaExp = 4000;
        const int GammaExp = 8000;

        #endregion

        public Monster()
        {
            ObjectType = GameObjectType.Monster;
        }
        public void Init(MonsterType type, int team)
        {
            if (!LoadMonsterData(type))
                return;

            MonsterTeam = team;
            DIST_TO_TARGET = DataManager.MonsterDict[type].attackDist;
            OnAttacked += HandlerRegisterTarget;
        }
        bool _appeared = false;
        public override void Update()
        {
            if (Room != null && _appeared == false)
            {
                ChangeState(FSMManager.Instance.GetAppearState());
                _appeared = true;
            }
            _currentState?.Execute(this);

            if (Target != null)
            {
                if (Target.State == CreatureState.Dead)
                {
                    Target = null;
                    ChangeState(FSMManager.Instance.GetIdleState());
                }
            }
        }

        #region State
        public void ChangeState(IMonsterState newState)
        {
            _currentState?.Exit(this);

            State = DetermineMonsterState(newState);

            _currentState = newState;
            _currentState?.Enter(this);
        }
        private CreatureState DetermineMonsterState(IMonsterState state)
        {
            if (state is IdleState)
                return CreatureState.Idle;
            else if (state is MovingState)
                return CreatureState.Moving;
            else if (state is SkillState || state is AimState)
                return CreatureState.Skill;
            else if (state is DeadState)
                return CreatureState.Dead;
            else if (state is AppearState)
                return CreatureState.Appear;

            return CreatureState.Idle;
        }
        public override void OnDead(GameObject attacker)
        {
            if (Room == null)
                return;

            State = CreatureState.Dead;
            ChangeState(new DeadState());

            // exp and score
            switch (Info.Monster.MonsterType)
            {
                case MonsterType.Drone:
                    attacker.Room.GetTeamExp(attacker.Team, DroneExp);
                    break;
                case MonsterType.Omega:
                    {
                        attacker.Room.GetTeamExp(attacker.Team, OmegaExp);

                        int OmegaScore = 5;
                        int Team = attacker.Team == 1 ? 2 : 1; // 상대팀 점수 차감
                        int score = attacker.Room.ReduceScore(Team, OmegaScore);
                        S_ChangeScore changeScorePacket = new S_ChangeScore();
                        changeScorePacket.Team = Team;
                        changeScorePacket.Score = score;
                        Room.Push(Room.Broadcast, changeScorePacket);
                    }
                    break;
                case MonsterType.Gamma:
                    {
                        attacker.Room.GetTeamExp(attacker.Team, GammaExp);

                        int GammaScore = 10;
                        int Team = attacker.Team == 1 ? 2 : 1; // 상대팀 점수 차감
                        int score = attacker.Room.ReduceScore(Team, GammaScore);
                        S_ChangeScore changeScorePacket = new S_ChangeScore();
                        changeScorePacket.Team = Team;
                        changeScorePacket.Score = score;
                        Room.Push(Room.Broadcast, changeScorePacket);
                    }
                    break;
            }
        }
        #endregion

        #region Hit
        private bool CheckTeam(GameObject attacker)
        {
            if (attacker is Player player)
                return (MonsterTeam == player.Team);
            return false;
        }
        protected override void OnDamaged(GameObject attacker, float damage, bool isBasicAttack = false)
        {
            if (CheckTeam(attacker))
                return;

            base.OnDamaged(attacker, damage, isBasicAttack);

            if (Target == null)
                OnAttacked?.Invoke(attacker);
        }
        public void OnHit(Creature creature)
        {
            if (Target == null)
                OnAttacked?.Invoke(creature);
        }
        private void HandlerRegisterTarget(GameObject attacker)
        {
            if (Target != null)
                return;

            if (attacker is Player attackerPlayer)
                Target = attackerPlayer;
        }
        // 몬스터가 때린 타겟
        public void OnTargetHit(Player target)
        {
            if (target is Creature creatureTarget)
                _currentState?.OnHit(this, creatureTarget);
        }
        #endregion

        #region Skill
        public MonsterSkillData CastRandomSkill()
        {
            int skillIdx = new Random().Next(0, _skills.Count);
            MonsterSkill skillName = _skills[skillIdx];

            if (DataManager.MonsterSkillDict.TryGetValue(skillName, out MonsterSkillData skillData) == false)
                return null;

            return skillData;
        }
        public void CreateHitbox(MonsterSkill skilltype)
        {
            Room?.CollManager?.AddHitbox(this, skilltype);
        }
        public ISkillBehavior CreateSkillBehavior(string behaviorName)
        {
            const string BehaviorNamespace = "Server.Game";
            string fullTypeName = $"{BehaviorNamespace}.{behaviorName}";

            Type type = Type.GetType(fullTypeName);
            if (behaviorName != null)
                type = Type.GetType(fullTypeName);

            if (type == null)
                return null;

            object instance = Activator.CreateInstance(type);
            return instance as ISkillBehavior;
        }

        public float CalcDamage(Creature attacker, Creature target)
        {
            Monster monsterAttacker = attacker as Monster;
            if (monsterAttacker == null)
                return 0f;
            if (!DataManager.MonsterSkillDict.ContainsKey(monsterAttacker.CurrentSkill))
                return 0f;

            if (target is Player)
                return DataManager.MonsterSkillDict[monsterAttacker.CurrentSkill].damage;
            else
                return 0f;
        }
        #endregion

        #region 범위 검색
        public bool IsInSkillRange()
        {
            if (Target == null)
                return false;

            Vector3 myPosition = PosInfo.ToVector();
            Vector3 targetPosition = Target.PosInfo.ToVector();
            return Vector3.Distance(myPosition, targetPosition) <= DIST_TO_TARGET;
        }

        public Player SearchForPlayerInRange()
        {
             return Room?.FindViableTarget(this, DIST_TO_TARGET);
        }
        public bool IsReturnSpawn()
        {
            Vector3 monsterPosition = PosInfo.ToVector();

            if (Vector3.Distance(monsterPosition, _spawnPosition) >= ACTIVE_RANGE)
                return true;

            if (Target == null)
                return true;

            if (Target != null && Target.State == CreatureState.Dead)
                return true;

            return false;
        }

        public bool IsAtSpawn()
        {
            var myPosition = PosInfo.ToVector();
            return (Vector3.Distance(myPosition, _spawnPosition) < DIST_TO_TARGET);
        }
        #endregion

        #region 패킷 전달
        public void PushState(CreatureState newState, PositionInfo posInfo = null, RotationInfo rotInfo = null, MonsterSkillData skillData = null, bool stateChange = true)
        {
             Room?.Push(() => BroadcastState(newState, posInfo, rotInfo, skillData, stateChange));
        }

        private void BroadcastState(CreatureState newState, PositionInfo posInfo = null, RotationInfo rotInfo = null, MonsterSkillData skillData = null, bool stateChange = true)
        {
            _sequenceId++;
            S_State statePacket = new S_State
            {
                ObjectId = Id,
                SequenceId = _sequenceId,
                MyState = newState,
                PosInfo = posInfo,
                RotInfo = rotInfo,
                ChangeState = stateChange
            };

            if (Target != null)
                statePacket.TargetPosition = Target.PosInfo;

            if (skillData != null)
            {
                statePacket.Skilltype = skillData.skillType;
                CurrentSkill = skillData.skillType;
            }

            Room?.Broadcast(statePacket);
        }
    
        #endregion

        #region 초기화
        private bool LoadMonsterData(MonsterType type)
        {
            if (DataManager.MonsterDict.TryGetValue(type, out MonsterData monsterData))
            {
                Stat.MergeFrom(monsterData.stat);
                Hp = MaxHp;
                _spawnPosition = new Vector3(PosInfo.PosX, PosInfo.PosY, PosInfo.PosZ);
                _spawnRotation = new Quaternion(RotInfo.Qx, RotInfo.Qy, RotInfo.Qz, RotInfo.Qw);
                State = CreatureState.Appear;
                if (monsterData.skills != null)
                    _skills.AddRange(monsterData.skills);
            }
            else
                return false;

            return true;
        }
#endregion
    }
}

