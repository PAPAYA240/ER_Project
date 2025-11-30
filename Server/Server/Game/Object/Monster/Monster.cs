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
        public bool ReturnToSpawn { get; set; }
        public Vector3 _lastTargetPos = new Vector3();

        // Detection
        private const float ACTIVE_RANGE = 20f;

        // Events
        public Action<GameObject> OnAttacked;

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

            if (DataManager.MonsterSkillDict.TryGetValue(MonsterSkill.MsGammaSkill1, out MonsterSkillData skillData) == false)
                return null;

            //Target?.Room?.Push(OnDamaged, this, skillData.damage + Attack, false);
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
            return Vector3.Distance(monsterPosition, _spawnPosition) >= ACTIVE_RANGE;
        }

        public bool IsAtSpawn()
        {
            var myPosition = PosInfo.ToVector();
            return (Vector3.Distance(myPosition, _spawnPosition) < 0.1f);
        }
        #endregion

        #region 패킷 전달
        public void PushState(CreatureState newState, PositionInfo posInfo = null, RotationInfo rotInfo = null, MonsterSkillData skillData = null)
        {
             Room?.Push(() => BroadcastState(newState, posInfo, rotInfo, skillData));
        }

        private void BroadcastState(CreatureState newState, PositionInfo posInfo = null, RotationInfo rotInfo = null, MonsterSkillData skillData = null)
        {
            _sequenceId++;
            S_State statePacket = new S_State
            {
                ObjectId = Id,
                SequenceId = _sequenceId,
                MyState = newState,
                PosInfo = posInfo,
                RotInfo = rotInfo
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

