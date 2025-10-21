using System;
using System.Collections.Generic;
using System.Numerics;
using System.Xml;
using Google.Protobuf.Protocol;
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
        public float _delaySkillAnimationTimer = 0;

        // Position
        public Vector3 spawnPosition = new Vector3();
        public bool ReturnToSpawn { get; set; }

        // Detection
        private const float SKILL_RANGE = 3.0f;
        private const float ACTIVE_RANGE = 100f;

        // Events
        public Action<GameObject> OnAttacked;
        public Action<GameObject, float> OnDamage;

        #endregion

        public Monster()
        {
            ObjectType = GameObjectType.Monster;
        }
        public void Init(string name)
        {
            if (!LoadMonsterData(name))
                return;

            OnAttacked += HandleAttacked;
            ChangeState(FSMManager.Instance.GetIdleState());
        }
       
        public override void Update()
        {
            if (IsStun && !(_currentState is IdleState))
            {
                ChangeState(new IdleState());
                return;
            }
            
            if (_currentState != null)
                _currentState?.Execute(this);
        }
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
            if (state is MovingState)
                return CreatureState.Moving;
            if (state is SkillState || state is AimState)
                return CreatureState.Skill;

            return CreatureState.Idle;
        }
        protected override void IdleState()
        {
             ChangeState(new IdleState());
        }

        public void CreateHitbox(MonsterSkill skilltype)
        {
            if(Room == null || Room.CollisionManager == null) return;

            Room.CollisionManager.AddHitbox(this, skilltype);
        }

        // 스킬 선택
        public MonsterSkillData Get_DecideAndUseSkill()
        {
            return DecideAndUseSkill();
        }
        protected MonsterSkillData DecideAndUseSkill()
        {
            int skillIdx = new Random().Next(0, _skills.Count);
            MonsterSkill skillName = _skills[skillIdx];

            if (DataManager.MonsterSkillDict.TryGetValue(skillName, out MonsterSkillData skillData) == false)
            {
                Console.WriteLine($"--> 사용할 스킬 ID({skillName})가 데이터에 없습니다.");
                return null;
            }

            if (Target == null || Target.Room == null)
                return skillData;

            Target.Room.Push(OnDamaged, this, skillData.damage + Attack, false);

            return skillData;
        }

        public override void OnDead(GameObject attacker)
        {
            if (Room == null)
                return;

            PosInfo.State = CreatureState.Dead;

            S_Die diePacket = new S_Die();
            diePacket.ObjectId = Id;
            diePacket.AttackerId = attacker.Id;
            Room.Broadcast(diePacket);
        }

        public void OnSkillHit(GameObject target)
        {
            if (target is Creature creatureTarget)
                _currentState?.OnHit(this, creatureTarget);
        }

        private void HandleAttacked(GameObject attacker)
        {
            if (attacker is Player attackerPlayer)
                 Target = attackerPlayer;
        }

        #region Helper Functions

        public bool IsFindTargetRange()
        {
            if (Target == null)
                return false;

            Vector3 monsterPos = new Vector3(PosInfo.PosX, PosInfo.PosY, PosInfo.PosZ);
            Vector3 targetPos = new Vector3(Target.PosInfo.PosX, Target.PosInfo.PosY, Target.PosInfo.PosZ);
            float distanceToTarget = Vector3.Distance(monsterPos, targetPos);

            return distanceToTarget <= _findRange;
        }
        public bool IsSkillRange() => IsPlayerInSkillRange();
        private bool IsPlayerInSkillRange()
        {
            if (Target == null)
                return false;

            Vector3 monsterPos = new Vector3(PosInfo.PosX, PosInfo.PosY, PosInfo.PosZ);
            Vector3 targetPos = new Vector3(Target.PosInfo.PosX, Target.PosInfo.PosY, Target.PosInfo.PosZ);
            float distanceToTarget = Vector3.Distance(monsterPos, targetPos);

            return distanceToTarget <= SKILL_RANGE;
        }

        public bool IsInSkillRange()
        {
            if (Target == null)
                return false;

            Vector3 myPosition = PosInfo.GetVector3FromPosInfo();
            Vector3 targetPosition = Target.PosInfo.GetVector3FromPosInfo();
            return Vector3.Distance(myPosition, targetPosition) <= SKILL_RANGE;
        }

        public bool IsReturnSpawn()
        {
            Vector3 monsterPosition = PosInfo.GetVector3FromPosInfo();
            return Vector3.Distance(monsterPosition, spawnPosition) >= ACTIVE_RANGE;
        }

        public bool IsAtSpawn()
        {
            var myPosition = PosInfo.GetVector3FromPosInfo();
            return (Vector3.Distance(myPosition, spawnPosition) < 0.1f);
        }

        // 활동 범위를 가지 않았는가?
        public bool IsArrivalSpawn()
        {
            Vector3 monsterPos = new Vector3(this.PosInfo.PosX, this.PosInfo.PosY, this.PosInfo.PosZ);
            float distanceToWaypoint = Vector3.Distance(monsterPos, spawnPosition);
            if (distanceToWaypoint < 0.1f)
                return true;
            return false;
        }

        private long GetCurrentTimeMs()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public ISkillBehavior CreateSkillBehavior(string behaviorName)
        {
            const string BehaviorNamespace = "Server.Game";
            string fullTypeName = $"{BehaviorNamespace}.{behaviorName}";

            Type type = Type.GetType(fullTypeName);
            if(behaviorName != null)
                type = Type.GetType(fullTypeName);

            if (type == null)
                return null;

            object instance = Activator.CreateInstance(type);
            return instance as ISkillBehavior;
        }
        #endregion

        #region 브로드캐스트
        public void PushState(CreatureState newState, PositionInfo posInfo = null, RotationInfo rotInfo = null, MonsterSkillData skillData = null)
        {
             Room?.Push(() => BroadcastState(newState, posInfo, rotInfo, skillData));
        }

        private void BroadcastState(CreatureState newState, PositionInfo posInfo = null, RotationInfo rotInfo = null, MonsterSkillData skillData = null)
        {
            if (State != newState)
                return;

            _sequenceId++;

            S_State statePacket = new S_State
            {
                ObjectId = Id,
                SequenceId = _sequenceId,
                MyState = State,
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

        #region 컴포넌트
        private bool LoadMonsterData(string name)
        {
            if (DataManager.MonsterDict.TryGetValue(name, out MonsterData monsterData))
            {
                Stat.MergeFrom(monsterData.stat);
                Hp = MaxHp;
                State = CreatureState.Idle;
                spawnPosition = new Vector3(PosInfo.PosX, PosInfo.PosY, PosInfo.PosZ);

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

