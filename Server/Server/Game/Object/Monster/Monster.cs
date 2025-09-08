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
        // 패킷 번호
        private int _sequenceId = 0;

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

        public override void Update()
        {
             _currentState?.Execute(this);
        }


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
        private float _findRange = 5.0f;
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
           
            // 몬스터가 1.5f 이내에 있고, 실제 스킬 범위 있는가?
            return distanceToTarget <= _skillRange;
        }

        public int _pathIdx = 0;
        long _nextCalcPathTick = 0;
        public void Get_CalculatePath() =>CalculatePath();
        private void CalculatePath()
        {
            if (Target == null || Target.Room != Room)
                return;

            Vector3 startPos = new Vector3(PosInfo.PosX, PosInfo.PosY, PosInfo.PosZ);
            Vector3 endPos = new Vector3(Target.PosInfo.PosX, Target.PosInfo.PosY, Target.PosInfo.PosZ);
            _path = Pathfinding.FindPath(startPos, endPos);
            _pathIdx = 0;

            if (_path != null && _path.Count > 0)
            {
                // 첫 웨이포인트가 현재 위치와 멀면 현재 위치도 경로에 넣어 자연스럽게 이동
                if (Vector3.Distance(_path[0], startPos) > 0.1f)
                {
                    _path.Insert(0, startPos);
                    _pathIdx = 0;
                }
            }
        }
        // 몬스터의 이동 로직을 담당하는 함수
        const float MOVE_STEP_INTERPOL = 3.0f;
        public void Get_MoveAlongPath() => MoveAlongPath();
        private void MoveAlongPath()
        {
            if (_path == null || _path.Count == 0)
                return;

            Vector3 monsterPos = new Vector3(PosInfo.PosX, PosInfo.PosY, PosInfo.PosZ);
            Console.WriteLine($"전 {monsterPos}");
            Vector3 nextWaypoint = _path[_pathIdx];
            Console.WriteLine($"후 {nextWaypoint}");

            // 실제 이동
            FollowToPlayer(nextWaypoint);

            // 이동 후 도착 체크
            Vector3 newMonsterPos = new Vector3(PosInfo.PosX, PosInfo.PosY, PosInfo.PosZ);

            float distanceToWaypoint = Vector3.Distance(newMonsterPos, nextWaypoint);

            if (distanceToWaypoint < MOVE_STEP_INTERPOL)
            {
                _pathIdx++;
                if (_pathIdx >= _path.Count)
                {
                    _path.Clear();
                    ChangeState(new IdleState());
                }
                // 다음 웨이포인트는 다음 틱에서 처리
            }

            // 이동 후 현재 상태 브로드캐스트
            BroadcastState(CreatureState.Moving, PosInfo, RotInfo);
        }

        private Vector3 MoveTowards(Vector3 current, Vector3 target, float maxDistanceDelta)
        {
            Vector3 toVector = target - current;
            float dist = toVector.Length();

            if (dist <= maxDistanceDelta || dist == 0f)
                return target;

            return current + toVector / dist * maxDistanceDelta;
        }

        private long _lastUpdateTime = 0;

        private float FIXED_MOVE_STEP = 0.8f;
        private void FollowToPlayer(Vector3 targetPos)
        {
            Vector3 monsterPos = new Vector3(PosInfo.PosX, PosInfo.PosY, PosInfo.PosZ);
            Vector3 dir = targetPos - monsterPos;
            float distanceSq = dir.LengthSquared();

            if (distanceSq < 0.0001f)
            {
                PosInfo.PosX = targetPos.X;
                PosInfo.PosY = targetPos.Y;
                PosInfo.PosZ = targetPos.Z;
                return;
            }

            // 첫 프레임에는 이동하지 않고 시간만 초기화
            long tick = Environment.TickCount64;
            if (_lastUpdateTime == 0)
            {
                _lastUpdateTime = tick;
                return;
            }

            // 경과 시간 계산
            double elapsedTime = (tick - _lastUpdateTime) / 1000.0;
            _lastUpdateTime = tick;

            float distance = (float)Math.Sqrt(distanceSq);
            float moveStep = Math.Min(FIXED_MOVE_STEP, distance);

            Vector3 dirNorm = dir / distance;
            Vector3 newPos = monsterPos + dirNorm * moveStep;

            PosInfo.PosX = newPos.X;
            PosInfo.PosY = newPos.Y;
            PosInfo.PosZ = newPos.Z;

            // 회전
            Vector3 PlayerPos = new Vector3(Target.PosInfo.PosX, Target.PosInfo.PosY, Target.PosInfo.PosZ);
            float distanceToTarget = Vector3.Distance(monsterPos, targetPos);
            Vector3 dirQ;
            if (distanceToTarget <= _findRange)
                dirQ = PlayerPos - newPos;
            else
                dirQ = targetPos - newPos;
            dirQ = Vector3.Normalize(dirQ);
            // 몬스터가 앞뒤로만 움직이도록 y축 값 제거
            Vector3 flatDir = new Vector3(dirQ.X, 0, dirQ.Z);

            if (flatDir.LengthSquared() > 0.0001f)
            {
                flatDir = Vector3.Normalize(flatDir);

                float angleRad = (float)Math.Atan2(flatDir.X, flatDir.Z);

                Quaternion targetRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, angleRad);

                float rotationSpeed = 2.0f;
                float t = (float)Math.Clamp(rotationSpeed * elapsedTime, 0f, 1f);

                Quaternion currentRotation = new Quaternion(RotInfo.Qx, RotInfo.Qy, RotInfo.Qz, RotInfo.Qw);
                Quaternion newRotation = Quaternion.Slerp(currentRotation, targetRotation, t);

                // 새 회전 정보 적용
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
            _sequenceId++;
            S_State statePacket = new S_State();
            statePacket.ObjectId = Id;
            statePacket.SequenceId = _sequenceId;

            statePacket.MyState = newState;

            // 스킬일 때는 위치/회전 정보를 덮어쓰지 않음
            if (skillData != null)
            {
                statePacket.Skilltype = skillData.skillType;
                _currentSkill = skillData.skillType;
                // 위치/회전 정보는 null로 유지
            }
            else
            {
                statePacket.PosInfo = posInfo;
                statePacket.RotInfo = rotInfo;
            }

            if (Room != null)
                Room.Broadcast(statePacket);
        }
        private long GetCurrentTimeMs()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
        #endregion
    }
}

