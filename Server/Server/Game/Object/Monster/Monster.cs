using System;
using System.Collections.Generic;
using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game.Object.Monster.AStar;
using Server.Game.Object.Monster.FSM;

namespace Server.Game.Object.Monster
{
    public interface IMonsterState
    {
        void Enter(Monster monster);
        void Execute(Monster monster);
        void Exit(Monster monster);
    }

    public class Monster : GameObject
    {
        List<MonsterSkill> _skills = new List<MonsterSkill>();
        MonsterSkill _currentSkill = MonsterSkill.MsNone;
        public Player Target { get; set; }
        public List<Vector3> _path = new List<Vector3>();
        public Vector3 _lastPlayerPosition = new Vector3();

        private IMonsterState _currentState;

        public Monster() => ObjectType = GameObjectType.Monster;
        public void Init(string name)
        {
            MonsterData monsterData = null;
            DataManager.MonsterDict.TryGetValue(name, out monsterData);

            Stat.MergeFrom(monsterData.stat);
            Stat.Hp = Stat.MaxHp;
            State = CreatureState.Idle;

            if(monsterData.skills != null)
                _skills.AddRange(monsterData.skills);

            ChangeState(new IdleState());
        }

        public void ChangeState(IMonsterState newState)
        {
            if (_currentState != null)
                _currentState.Exit(this);

            _currentState = newState;
            if (_currentState != null)
                _currentState.Enter(this);
        }

        public override void Update() => _currentState?.Execute(this);


        // 스킬 탐색
        public long _lastSkillTime = 0; // 마지막 스킬 사용 시간
        public MonsterSkillData Get_DecideAndUseSkill()
        {
            return DecideAndUseSkill();
        }
        protected MonsterSkillData DecideAndUseSkill()
        {
            if (_skills.Count == 0)
                return null;

            int skillIdx = new Random().Next(0, _skills.Count);
            MonsterSkill skillName = _skills[skillIdx];

            MonsterSkillData skillData = null;
            if (DataManager.MonsterSkillDict.TryGetValue(skillName, out skillData) == false)
            {
                Console.WriteLine($"--> 사용할 스킬 ID({skillName})가 데이터에 없습니다.");
                return null;
            }

            Target.OnDamaged(this, skillData.damage + Stat.Attack);
            return skillData;
        }

        protected virtual void UpdateDead()
        {
            // TODO: 몬스터 사망 시 처리
            State =CreatureState.Dead;
        }

        #region Helper Functions

        private float _skillRange = 0.5f;
        private float _findRange = 1.5f;
        public bool IsSkillRange() => IsPlayerInSkillRange();
        private bool IsPlayerInSkillRange()
        {
            if (Target == null)
                return false;

            Vector3 monsterPos = new Vector3(PosInfo.PosX, PosInfo.PosY, PosInfo.PosZ);
            Vector3 targetPos = new Vector3(Target.PosInfo.PosX, Target.PosInfo.PosY, Target.PosInfo.PosZ);
            float distanceToTarget = Vector3.Distance(monsterPos, targetPos);

            // if(distanceToTarget <= _findRange)
            //     Console.WriteLine("1.5 이내");
            Console.WriteLine($"{distanceToTarget}");
            Console.WriteLine($"Monster : {monsterPos}");
            Console.WriteLine($"Player : {targetPos}");
            // 몬스터가 1.5f 이내에 있고, 실제 스킬 범위 있는가?
            return distanceToTarget <= _skillRange;
        }

        int _pathIdx = 0;
        long _nextCalcPathTick = 0;
        public void Get_CalculatePath() =>CalculatePath();
        private void CalculatePath()
        {
            Vector3 startPos = new Vector3(PosInfo.PosX, PosInfo.PosY, PosInfo.PosZ);
            Vector3 endPos = new Vector3(Target.PosInfo.PosX, Target.PosInfo.PosY, Target.PosInfo.PosZ);

            _path = Pathfinding.FindPath(startPos, endPos);
            _pathIdx = 0;

            if (_path == null || _path.Count == 0)
            {
                Console.WriteLine($"경로 없음 추적 포기");
                Target = null;
                //State = CreatureState.Idle;
            }
        }

        // 몬스터의 이동 로직을 담당하는 함수
        public void Get_MoveAlongPath() => MoveAlongPath();
        private void MoveAlongPath()
        {
            if (_path == null)
                return;
            // 경로 인덱스가 유효하지 않으면 종료
            if (_pathIdx >= _path.Count)
            {
                _path.Clear();
                State = CreatureState.Idle;
                return;
            }

            // 다음 웨이포인트로 이동
            Vector3 nextWaypoint = _path[_pathIdx];
            FollowToPlayer(nextWaypoint);

            Vector3 monsterPos = new Vector3(PosInfo.PosX, PosInfo.PosY, PosInfo.PosZ);

            // 웨이포인트에 충분히 가까워졌는지 확인
            float distToNextWaypoint = Vector3.Distance(monsterPos, nextWaypoint);
            if (distToNextWaypoint < 1.0f)
            {
                // 다음 웨이포인트로 인덱스 증가
                _pathIdx++;
            }
        }

        private long _lastUpdateTime = 0;
        private void FollowToPlayer(Vector3 targetPos)
        {
            if (_lastUpdateTime == 0)
                _lastUpdateTime = Environment.TickCount64;

            Vector3 monsterPos = new Vector3(PosInfo.PosX, PosInfo.PosY, PosInfo.PosZ);
            Vector3 dir = targetPos - monsterPos;
            float distance = dir.Length();

            if (dir.LengthSquared() < 0.1f)
            {
                PosInfo.PosX = targetPos.X;
                PosInfo.PosY = targetPos.Y;
                PosInfo.PosZ = targetPos.Z;
                return;
            }

            long tick = Environment.TickCount64;
            float elapsedTime = (tick - _lastUpdateTime) / 1000.0f;
            _lastUpdateTime = tick;

            float moveStep = Stat.Speed * elapsedTime;
            moveStep = Math.Min(moveStep, distance);

            // 이동
            dir = Vector3.Normalize(dir);
            Vector3 newPos = monsterPos + dir * moveStep;

            PosInfo.PosX = newPos.X;
            PosInfo.PosY = newPos.Y;
            PosInfo.PosZ = newPos.Z;

            // 회전
            Vector3 flatDir = new Vector3(dir.X, 0, dir.Z);
            if (flatDir.LengthSquared() > 0.0001f)
            {
                Quaternion currentRotation = new Quaternion(RotInfo.Qx, RotInfo.Qy, RotInfo.Qz, RotInfo.Qw);
                float angleRad = (float)Math.Atan2(flatDir.X, flatDir.Z);
                Quaternion targetRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, angleRad);

                float rotationSpeed = 50.0f;
                Quaternion newRotation = Quaternion.Slerp(currentRotation, targetRotation, rotationSpeed * elapsedTime);

                RotInfo.Qx = newRotation.X;
                RotInfo.Qy = newRotation.Y;
                RotInfo.Qz = newRotation.Z;
                RotInfo.Qw = newRotation.W;
            }
        }

        void BroadcastSkill(SkillData skillData)
        {
            S_Skill skill = new S_Skill() { SkillInfo = new SkillInfo() };
            skill.ObjectId = Id;
            skill.SkillInfo.SkillId = skillData.id;

            State = CreatureState.Skill;
            Room.Broadcast(skill);
        }

        void BroadcastMove()
        {
            S_Move movePacket = new S_Move();
            movePacket.ObjectId = Id;
            movePacket.PosInfo = new PositionInfo(PosInfo);
            movePacket.RotInfo = new RotationInfo(RotInfo);

            State = CreatureState.Moving;
            Room.Broadcast(movePacket);
        }

        public void BroadcastState(CreatureState newState, PositionInfo posInfo = null, RotationInfo rotInfo = null, MonsterSkillData skillData = null)
        {
            State = newState;

            S_State statePacket = new S_State();
            statePacket.ObjectId = Id;
            
            // State
            statePacket.MyState = newState;

            statePacket.PosInfo = posInfo;
            statePacket.RotInfo = rotInfo;

            // Skill
            if(skillData != null)
            { 
                statePacket.Skilltype = skillData.skillType;
                _currentSkill = skillData.skillType;
                statePacket.PosInfo = PosInfo;
                statePacket.RotInfo = RotInfo;
            }

            if(Room != null)
                Room.Broadcast(statePacket);
        }
        private long GetCurrentTimeMs()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
        #endregion
    }
}

