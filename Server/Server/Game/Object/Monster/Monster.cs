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

            // 몬스터를 마지막에 공격한 Player를 타겟으로 잡자
            Player target = Room.FindPlayer(p =>
            {
                Vector3 playerPos = new Vector3(p.PosInfo.PosX, p.PosInfo.PosY, p.PosInfo.PosZ);
                Vector3 monsterPos = new Vector3(PosInfo.PosX, PosInfo.PosY, PosInfo.PosZ);
                return Vector3.Distance(monsterPos, playerPos) <= _searchCellDist;
            });

            if (target == null)
                return;

            _target = target;
            State = CreatureState.Moving;
            Console.WriteLine("--> 타겟 찾음! 이동 시작.");
        }

        // Moving 상태 
        long _nextMoveTick = 0;
        protected virtual void UpdateMoving()
        {
            if (_nextMoveTick > Environment.TickCount64)
                return;

            long tick = (long)(1000 / Speed);
            _nextMoveTick = Environment.TickCount64 + tick;
            if (_target == null || _target.Room != Room/* || _target.Hp == 0*/)
            {
                _target = null;
                _path.Clear();
                State = CreatureState.Idle;
                return;
            }

            if (IsPlayerInSkillRange())
            {
                State = CreatureState.Skill;
                _path.Clear();
                return;
            }

            Console.WriteLine("추적");
            AStar();
            BroadcastMove();
        }

        // 스킬 시전
        private long  _skillEndTime = 0; // 스킬 '애니메이션'이 끝나는 시간
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
        private void AStar()
        {
            if (_path.Count == 0 || _nextCalcPathTick < Environment.TickCount64)
            {
                _nextCalcPathTick = Environment.TickCount64 + 1000;

                Vector3 startPos = new Vector3(PosInfo.PosX, PosInfo.PosY, PosInfo.PosZ);
                Vector3 endPos = new Vector3(_target.PosInfo.PosX, _target.PosInfo.PosY, _target.PosInfo.PosZ);

                _path = Pathfinding.FindPath(startPos, endPos);
                _pathIdx = 0;

                // 경로 X 추적 포기
                if (_path.Count == 0)
                {
                    State = CreatureState.Idle;
                    return;
                }
            }

            if (_path.Count > 0)
            {
                Vector3 targetPos = _path[_pathIdx];
                // TODO : FollowToTarget 삽입
                FollowToPlayer(targetPos);

                float dist = Vector3.Distance(new Vector3(PosInfo.PosX, PosInfo.PosY, PosInfo.PosZ), targetPos);
                if (dist < 0.5f)
                {
                    _pathIdx++;
                    // 도차쿠
                    if (_pathIdx >= _path.Count)
                    {
                        _path.Clear();
                        State = CreatureState.Idle;
                    }
                }
            }
        }

        private void FollowToPlayer(Vector3 targetPos)
        {
            Vector3 monsterPos = new Vector3(PosInfo.PosX, PosInfo.PosY, PosInfo.PosZ);
            Vector3 dir = targetPos - monsterPos;

            if (dir.LengthSquared() < 0.0001f)
                return;

            long tick = (long)(1000 / Speed);
            float tickSeconds = tick / 1000.0f;
            Vector3 moveDir = Vector3.Normalize(dir);
            float moveDist = Speed * tickSeconds;

            PosInfo.PosX += moveDir.X * moveDist;
            //PosInfo.PosY += moveDir.Y * moveDist;
            PosInfo.PosZ += moveDir.Z * moveDist;

            // 회전
            Vector3 flatDir = new Vector3(dir.X, 0, dir.Z);
            if (flatDir.LengthSquared() > 0.0001f)
            {
                Quaternion currentRotation = new Quaternion(RotInfo.Qx, RotInfo.Qy, RotInfo.Qz, RotInfo.Qw);
                float angleRad = (float)Math.Atan2(flatDir.X, flatDir.Z);
                Quaternion targetRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, angleRad);

                float rotationSpeed = 5.0f;
                float deltaTime = (float)tick / 1000.0f;
                Quaternion newRotation = Quaternion.Lerp(currentRotation, targetRotation, rotationSpeed * deltaTime);
                
                RotInfo.Qx = targetRotation.X;
                RotInfo.Qy = targetRotation.Y;
                RotInfo.Qz = targetRotation.Z;
                RotInfo.Qw = targetRotation.W;
            }
        }

        void BroadcastSkill(SkillData skillData)
        {
            S_Skill skill = new S_Skill() { Info = new SkillInfo() };
            skill.ObjectId = Id;
            skill.Info.SkillId = skillData.id;
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

