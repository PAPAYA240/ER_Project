using System;
using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game.Object.Monster
{
    public class Monster : GameObject
    {
        public Monster()
        {
            ObjectType = GameObjectType.Monster;

            // TEMP
            Stat.Level = 1;
            Stat.Hp = 100;
            Stat.MaxHp = 100;
            Stat.Speed = 5f;

            State = CreatureState.Idle;
        }

        // FSM
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
            State = CreatureState.Moving;
            Console.WriteLine("--> 타겟 찾음! 이동 시작.");
        }

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
                State = CreatureState.Idle;
                return;
            }

            if (IsPlayerInSkillRange())
            {
                // TODO : 스킬 쿨 타임 고민해야 함ㅁ
                if (_skillCooldownEndTimeMs < GetCurrentTimeMs())
                {
                    Console.WriteLine("DecideAndUseSkill");
                    DecideAndUseSkill();
                }
                else
                    return;
            }
            else
            {
                // 사거리 밖이면 계속 추적
                Console.WriteLine("추적");
                FollowToPlayer();
                BroadcastMove();
            }
        }

        private long _skillAnimationEndTimeMs = 0; // 스킬 '애니메이션'이 끝나는 시간
        private long _skillCooldownEndTimeMs = 0;  // 다음 스킬을 '사용'할 수 있는 시간 (쿨타임)

        protected virtual void UpdateSkill()
        {
            if (GetCurrentTimeMs() > _skillAnimationEndTimeMs)
            {
                State = CreatureState.Idle; 
                Console.WriteLine("--> 스킬 애니메이션 종료. Idle로 전환.");
            }
        }

        protected virtual void DecideAndUseSkill()
        {
            int skillId = new Random().Next(1, 4); 
            Skill skillData = null;
            if (DataManager.MonsterSkillDict.TryGetValue(skillId, out skillData) == false)
            {
                Console.WriteLine($"--> 사용할 스킬 ID({skillId})가 데이터에 없습니다.");
                return;
            }

            Console.WriteLine($"--> 사거리 진입! {skillData.name} 스킬 사용!");
            State = CreatureState.Skill;

            _skillAnimationEndTimeMs = GetCurrentTimeMs() + (long)(skillData.cooldown * 1000);
            _skillCooldownEndTimeMs = GetCurrentTimeMs() + (long)(skillData.cooldown * 1000);
            BroadcastSkill(skillData);
            _target.OnDamaged(this, skillData.damage + Stat.Attack);
        }

        protected virtual void UpdateDead()
        {
            // TODO: 몬스터 사망 시 처리
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

        private void FollowToPlayer()
        {
            Vector3 monsterPos = new Vector3(PosInfo.PosX, PosInfo.PosY, PosInfo.PosZ);
            Vector3 targetPos = new Vector3(_target.PosInfo.PosX, _target.PosInfo.PosY, _target.PosInfo.PosZ);
            Vector3 dir = targetPos - monsterPos;

            if (dir.LengthSquared() < 0.0001f)
                return;

            long tick = (long)(1000 / Speed);
            float tickSeconds = tick / 1000.0f;
            Vector3 moveDir = Vector3.Normalize(dir);
            float moveDist = Speed * tickSeconds;

            PosInfo.PosX += moveDir.X * moveDist;
            PosInfo.PosY += moveDir.Y * moveDist;
            PosInfo.PosZ += moveDir.Z * moveDist;

            Vector3 flatDir = new Vector3(dir.X, 0, dir.Z);
            if (flatDir.LengthSquared() > 0.0001f)
            {
                float angleRad = (float)Math.Atan2(flatDir.X, flatDir.Z);
                Quaternion targetRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, angleRad);
                RotInfo.Qx = targetRotation.X;
                RotInfo.Qy = targetRotation.Y;
                RotInfo.Qz = targetRotation.Z;
                RotInfo.Qw = targetRotation.W;
            }
        }

        void BroadcastSkill(Skill skillData)
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

