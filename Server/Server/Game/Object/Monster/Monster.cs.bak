using System;
using System.Collections.Generic;
using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using static Server.Data.DataUtils;

namespace Server.Game
{
    public interface IMonsterState
    {
        void Enter(Monster monster);
        void Execute(Monster monster);
        void Exit(Monster monster);
    }

    public class Monster : Creature
    {
        // 패킷
        private int _sequenceId = 0;

        // Monster 정보
        List<MonsterSkill> _skills = new List<MonsterSkill>();  // 사용 가능한 스킬 목록
        public MonsterSkill CurrentSkill { get; private set; } // 현재 사용 중인 스킬
        private IMonsterState _currentState;
        public Vector3 spawnPosition = new Vector3();

        // 탐지 정보
        private const float _skillRange = 3.0f;

        // TODO : 감마 총알 예시
        public float _delaySkillAnimationTimer = 0;

        public Action<GameObject> OnAttacked;

        public Monster() => ObjectType = GameObjectType.Monster;

        public Action<GameObject, float> OnDamage;
        public void Init(string name)
        {
            if (!Add_MonsterData(name))
                return;
            this.OnAttacked += HandleAttacked;
            ChangeState(new IdleState());
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
            if (_currentState != null)
                _currentState.Exit(this);

            _currentState = newState;
            if (_currentState != null)
                _currentState.Enter(this);
        }

        protected override void IdleState()
        {
             ChangeState(new IdleState());
        }

        public void MonsterCollision(MonsterSkill skilltype)
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

            return distanceToTarget <= _skillRange;
        }

        const float _activeRange = 30f;
        // 다시 스폰 장소로 돌아가는가? 
        public bool IsReturnSpawn()
        {
            Vector3 monsterPos = new Vector3(this.PosInfo.PosX, this.PosInfo.PosY, this.PosInfo.PosZ);
            float dist = Vector3.Distance(monsterPos, spawnPosition);
            if (_activeRange <= dist)
            {
                Console.WriteLine("ReturnSpawn");
                return true;
            }
            return false;
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
        #endregion

        #region 브로드캐스트
        public void PushState(CreatureState newState, PositionInfo posInfo = null, RotationInfo rotInfo = null, MonsterSkillData skillData = null)
        {
            if(Room != null)
                Room.Push(() => BroadcastState(newState, posInfo, rotInfo, skillData));
        }

        private void BroadcastState(CreatureState newState, PositionInfo posInfo = null, RotationInfo rotInfo = null, MonsterSkillData skillData = null)
        {
            _sequenceId++;
            State = newState;

            S_State statePacket = new S_State();
            statePacket.ObjectId = Id;
            statePacket.SequenceId = _sequenceId;

            statePacket.MyState = newState;

            if (Target != null)
                statePacket.TargetPosition = Target.PosInfo;
            if (skillData != null)
            {
                statePacket.Skilltype = skillData.skillType;
                CurrentSkill = skillData.skillType;
            }

            statePacket.PosInfo = posInfo;
            statePacket.RotInfo = rotInfo;

            if (Room != null)
                Room.Broadcast(statePacket);
        }
        #endregion

        #region 컴포넌트
        private bool Add_MonsterData(string name)
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

