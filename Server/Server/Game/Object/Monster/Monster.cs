using System;
using System.Collections.Generic;
using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game.Object.Monster.AStar;

namespace Server.Game.Object.Monster
{
    public class Monster : GameObject
    {
        List<string> _skills = new List<string>();
        public Monster()
        {
            ObjectType = GameObjectType.Monster;
        }

        public void Init(string name)
        {
            MonsterData monsterData = null;
            DataManager.MonsterDict.TryGetValue(name, out monsterData);

            Stat.MergeFrom(monsterData.stat);
            Stat.Hp = Stat.MaxHp;
            State = CreatureState.Idle;

            if(monsterData.skills != null)
                _skills.AddRange(monsterData.skills);
        }

        public override void Update()
        {
            switch (State)
            {
                case CreatureState.Idle:
                    UpdateIdle();
                    break;
                case CreatureState.Moving:
                    UpdateMoving();
                    break;
                case CreatureState.Skill:
                    UpdateSkill();
                    break;
                case CreatureState.Dead:
                    UpdateDead();
                    break;
            }
        }

        // Idle 상태 
        Player _target;
        int _searchCellDist = 10;
        long _nextSearchTick = 0;
        protected virtual void UpdateIdle()
        {
            if (_nextSearchTick > Environment.TickCount64)
                return;
            _nextSearchTick = Environment.TickCount64 + 1000;

            Player target = Room.FindPlayer(p =>
            {
                Vector3 playerPos = new Vector3(p.PosInfo.PosX, p.PosInfo.PosY, p.PosInfo.PosZ);
                Vector3 monsterPos = new Vector3(PosInfo.PosX, PosInfo.PosY, PosInfo.PosZ);
                return Vector3.Distance(monsterPos, playerPos) <= _searchCellDist;
            });

            if (target == null)
                return;

            _target = target;

            // 목표를 찾으면 경로 한 번 계산
            CalculatePath();

            State = CreatureState.Moving;
            Console.WriteLine("--> 타겟 찾음! 이동 시작.");
        }

        // Moving 상태 
        long _nextMoveTick = 0;
        protected virtual void UpdateMoving()
        {
            if (_nextMoveTick > Environment.TickCount64)
                return;

            _nextMoveTick = Environment.TickCount64 + 100;

            if (_target == null || _path.Count == 0 || _target.Room != Room)
            {
                _target = null;
                State = CreatureState.Idle;
                _path.Clear();
                return;
            }

            // 스킬 범위 내에 들어오면 스킬 상태로 전환
            if (IsPlayerInSkillRange())
            {
                State = CreatureState.Skill;
                _path.Clear();
                return;
            }

            Console.WriteLine("추적");

            // 이동만 담당하는 함수 
            MoveAlongPath();

            BroadcastMove();
        }

        // 스킬 시전
        private long  _skillEndTime = 0;
        bool _selectSkill = false;
        protected virtual void UpdateSkill()
        {
            if (_selectSkill)
            {
                if (_skillEndTime > Environment.TickCount64)
                    return;
                State = CreatureState.Moving;
                _selectSkill = false;
            }
            else
            {
                _selectSkill = true;
                DecideAndUseSkill();
            }
        }

        // 스킬 탐색
        protected virtual void DecideAndUseSkill()
        {
            if (_skills.Count == 0)
                return;

            State = CreatureState.Skill;
            int skillIndex = new Random().Next(0, _skills.Count);
            string skillName = _skills[skillIndex];

            SkillData skillData = null;
            if (DataManager.MonsterSkillDict.TryGetValue(skillName, out skillData) == false)
            {
                Console.WriteLine($"--> 사용할 스킬 ID({skillName})가 데이터에 없습니다.");
                return;
            }

            Console.WriteLine($"--> 사거리 진입! {skillData.name} 스킬 사용!");
            State = CreatureState.Skill;

            long animationTick = (long)(skillData.animationTime * 1000);
            _skillEndTime = Environment.TickCount64 + animationTick;

            BroadcastSkill(skillData);
            _target.OnDamaged(this, skillData.damage + Stat.Attack);
        }

        protected virtual void UpdateDead()
        {
            // TODO: 몬스터 사망 시 처리
            State =CreatureState.Dead;
        }

        #region Helper Functions

        private float _skillRange = 1.5f;
        private bool IsPlayerInSkillRange()
        {
            if (_target == null)
                return false;

            Vector3 monsterPos = new Vector3(PosInfo.PosX, PosInfo.PosY, PosInfo.PosZ);
            Vector3 targetPos = new Vector3(_target.PosInfo.PosX, _target.PosInfo.PosY, _target.PosInfo.PosZ);
            return Vector3.Distance(monsterPos, targetPos) <= _skillRange;
        }

        List<Vector3> _path = new List<Vector3>();
        int _pathIdx = 0;
        long _nextCalcPathTick = 0;
        private void CalculatePath()
        {
            Vector3 startPos = new Vector3(PosInfo.PosX, PosInfo.PosY, PosInfo.PosZ);
            Vector3 endPos = new Vector3(_target.PosInfo.PosX, _target.PosInfo.PosY, _target.PosInfo.PosZ);

            _path = Pathfinding.FindPath(startPos, endPos);
            _pathIdx = 0;

            if (_path.Count == 0)
            {
                Console.WriteLine($"경로 없음 추적 포기");
                _target = null;
                State = CreatureState.Idle;
            }
        }

        // 몬스터의 이동 로직을 담당하는 함수
        private void MoveAlongPath()
        {
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
            Console.WriteLine($"Current Position: {monsterPos}, Next Waypoint: {nextWaypoint}, Distance: {distToNextWaypoint}");
            if (distToNextWaypoint < 0.5f)
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

                float rotationSpeed = 10.0f;
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
            Room.Broadcast(skill);
        }

        void BroadcastMove()
        {
            S_Move movePacket = new S_Move();
            movePacket.ObjectId = Id;
            movePacket.PosInfo = new PositionInfo(PosInfo);
            movePacket.RotInfo = new RotationInfo(RotInfo);
            Room.Broadcast(movePacket);
        }
        private long GetCurrentTimeMs()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
        #endregion
    }
}

