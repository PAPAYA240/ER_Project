using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game
{
    public interface IMonsterState
    {
        void Enter(Monster monster);
        void Execute(Monster monster);
        void Exit(Monster monster);
    }

    public class Monster : GameObject
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
        private const float _findRange = 30.0f;

        // TODO : 감마 총알 예시
        public float _delaySkillAnimationTimer = 0;


        // Targeting
        public Player PlayerTarget { get; set; }

        public List<Vector3> _path = new List<Vector3>();

        public Monster() => ObjectType = GameObjectType.Monster;

        public void Init(string name)
        {
            if (!Add_MonsterData(name))
                return;

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
            if(_currentState != null)
                _currentState?.Execute(this);
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

            PlayerTarget.Room.Push(OnDamaged, this, skillData.damage + Attack);
            //PlayerTarget.OnDamaged(this, skillData.damage + Stat.Attack);

            return skillData;
        }

        protected virtual void UpdateDead()
        {
            // TODO: 몬스터 사망 시 처리
            State =CreatureState.Dead;
        }

        #region Helper Functions

        public bool IsFindTargetRange()
        {
            if (PlayerTarget == null)
                return false;

            Vector3 monsterPos = new Vector3(PosInfo.PosX, PosInfo.PosY, PosInfo.PosZ);
            Vector3 targetPos = new Vector3(PlayerTarget.PosInfo.PosX, PlayerTarget.PosInfo.PosY, PlayerTarget.PosInfo.PosZ);
            float distanceToTarget = Vector3.Distance(monsterPos, targetPos);

            return distanceToTarget <= _findRange;
        }
        public bool IsSkillRange() => IsPlayerInSkillRange();
        private bool IsPlayerInSkillRange()
        {
            if (PlayerTarget == null)
                return false;

            Vector3 monsterPos = new Vector3(PosInfo.PosX, PosInfo.PosY, PosInfo.PosZ);
            Vector3 targetPos = new Vector3(PlayerTarget.PosInfo.PosX, PlayerTarget.PosInfo.PosY, PlayerTarget.PosInfo.PosZ);
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
            {
                return true;
            }
                Console.WriteLine("활동 범위를 넘어갔는가?");
            return false;
        }

        public Player FindTarget(Monster monster)
        {
            // 플레이어 판단
            monster.PlayerTarget = monster.Room.FindPlayer(p =>
            {
                Vector3 playerPos = new Vector3(p.PosInfo.PosX, p.PosInfo.PosY, p.PosInfo.PosZ);
                Vector3 monsterPos = new Vector3(monster.PosInfo.PosX, monster.PosInfo.PosY, monster.PosInfo.PosZ);

                Monster targetMonster = p.Target as Monster;
                if (targetMonster == this)
                    return true;
                else
                    return false;
            });
            return monster.PlayerTarget;
        }

        public int _pathIdx = 0;
        public void Get_CalculatePath(Vector3 targetPos) =>CalculatePath(targetPos);
        private void CalculatePath(Vector3 targetPos)
        {
            Vector3 startPos = new Vector3(PosInfo.PosX, PosInfo.PosY, PosInfo.PosZ);
            _path = Pathfinding.FindPath(startPos, targetPos);
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
        public void Get_MoveAlongPath() => MoveAlongPath();
        private void MoveAlongPath()
        {
            if (_path == null || _path.Count == 0)
                return;

            Vector3 monsterPos = new Vector3(PosInfo.PosX, PosInfo.PosY, PosInfo.PosZ);
            Vector3 nextWaypoint = _path[_pathIdx];

            if (CheckArrival(nextWaypoint))
            {
                _pathIdx++;
                if (_pathIdx >= _path.Count)
                {
                    _path.Clear();
                    ChangeState(new IdleState());
                }
            }

            // 실제 이동
            FollowToTarget(nextWaypoint);
            PushState(CreatureState.Moving, PosInfo, RotInfo);

        }

        const float MOVE_STEP_INTERPOL = 3.0f;
        private bool CheckArrival(Vector3 targetPos)
        {
            Vector3 monsterPos = new Vector3(PosInfo.PosX, PosInfo.PosY, PosInfo.PosZ);
            float distanceToWaypoint = Vector3.Distance(monsterPos, targetPos);

            return distanceToWaypoint < MOVE_STEP_INTERPOL;

        }

        private long _lastUpdateTime = 0;
        private const float FIXED_MOVE_STEP = 0.8f;
        public void FollowToTarget(Vector3 targetPos)
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

            // =========== 경과 시간 계산 =========== 
            double elapsedTime = (tick - _lastUpdateTime) / 1000.0;
            _lastUpdateTime = tick;

            float distance = (float)Math.Sqrt(distanceSq);
            float moveStep = Math.Min(FIXED_MOVE_STEP, distance);

            Vector3 dirNorm = dir / distance;
            Vector3 newPos = monsterPos + dirNorm * moveStep;

            PosInfo.PosX = newPos.X;
            PosInfo.PosY = newPos.Y;
            PosInfo.PosZ = newPos.Z;

            // =========== 회전 =========== 
            Vector3 dirQ = new Vector3();
            if (PlayerTarget != null)
            {
                Vector3 PlayerPos = new Vector3(PlayerTarget.PosInfo.PosX, PlayerTarget.PosInfo.PosY, PlayerTarget.PosInfo.PosZ);
                float distanceToTarget = Vector3.Distance(monsterPos, targetPos);
                if (distanceToTarget <= _findRange)
                    dirQ = PlayerPos - monsterPos;
            }
            else
            {
                dirQ = targetPos - monsterPos;
            }
            LookAtTarget(dirQ, elapsedTime);
        }

        // 거리, 시간, 보간 여부, 회전 속도
        public void LookAtTarget(Vector3 dirQ, double elapsedTime, bool isSlerp = true, float rotationSpeed = 2.0f)
        {
            // 방향 벡터가 너무 작으면 회전하지 않음
            if (dirQ.LengthSquared() < 0.0001f)
                return; 

            dirQ= Vector3.Normalize(dirQ);
            Vector3 flatDir = new Vector3(dirQ.X, 0, dirQ.Z);
            flatDir = Vector3.Normalize(flatDir);

            // [3] 회전 각도 및 쿼터니언 계산
            float angleRad = (float)Math.Atan2(flatDir.X, flatDir.Z);
            Quaternion targetRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, angleRad);
            
            Quaternion newRotation;
            if (isSlerp)
            {
                // [4] 부드러운 회전 보간 (Slerp)
                float t = (float)Math.Clamp(rotationSpeed * elapsedTime, 0f, 1f);
                Quaternion currentRotation = new Quaternion(RotInfo.Qx, RotInfo.Qy, RotInfo.Qz, RotInfo.Qw);
                newRotation = Quaternion.Slerp(currentRotation, targetRotation, t);
            }
            else
                newRotation = targetRotation;

            // [5] 몬스터의 회전 정보 업데이트
            RotInfo.Qx = newRotation.X;
            RotInfo.Qy = newRotation.Y;
            RotInfo.Qz = newRotation.Z;
            RotInfo.Qw = newRotation.W;
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

            if (PlayerTarget != null)
                statePacket.TargetPosition = PlayerTarget.PosInfo;
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

