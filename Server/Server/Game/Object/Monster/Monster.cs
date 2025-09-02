using System;
using System.Numerics;
using Google.Protobuf.Protocol;

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
        int _searchCellDist = 100;
        int _chaseCellDist = 20;

        int _skillRange = 1;
        long _nextSearchTick = 0;
        protected virtual void UpdateIdle()
        {
            if (_nextSearchTick > Environment.TickCount64)
                return;
            _nextSearchTick = Environment.TickCount64 + 1000;

            Vector3 monsterPos = new Vector3(PosInfo.PosX, PosInfo.PosY,PosInfo.PosZ);

            Player target = Room.FindPlayer(p =>
            {
                Vector3 playerPos = new Vector3(p.PosInfo.PosX,p.PosInfo.PosY, p.PosInfo.PosZ);
                float distance = Vector3.Distance(monsterPos, playerPos);
                return Vector3.Distance(monsterPos, playerPos) <= _searchCellDist;
             });

            if (target == null)
            {
                Console.WriteLine("--> 결과: 타겟 없음");
                return;
            }

            Console.WriteLine($"--> 결과: 타겟 찾음! ID: {target.Id}");
            _target = target;
            State = CreatureState.Moving;
        }

        long _nextMoveTick = 0;
        float range = 0.2f;
        protected virtual void UpdateMoving()
        {
            // == 임시 ==
            if (_nextSearchTick > Environment.TickCount64)
                return;
            long tick = (long)(1000 / Speed);
            _nextMoveTick = Environment.TickCount64 + tick;

            if (_target == null || _target.Room != Room || _target.Hp == 0)
            {
                Console.WriteLine("--> 타겟을 잃었거나 타겟이 죽었슴다. Idle 상태로 돌아갑니다.");
                _target = null;
                State = CreatureState.Idle;
                return;
            }

            Vector3 monsterPos = new Vector3(PosInfo.PosX, PosInfo.PosY, PosInfo.PosZ);
            Vector3 targetPos  = new Vector3(_target.PosInfo.PosX, _target.PosInfo.PosY, _target.PosInfo.PosZ);
            Vector3 dir = targetPos - monsterPos;
            float dist = dir.Length();
            if (dist < range)
            {
                Console.WriteLine("--> Monster Skill 쓰는 중");
                State = CreatureState.Skill;
                return;
            }
            //if (dist > _chaseCellDist)
            //{
            //    _target = null;
            //    State = CreatureState.Idle;
            //    return;
            //}

            //if (dist <= _skillRange)
            //{
            //    State = CreatureState.Skill;
            //    return;
            //}

            // 거리 계산
            Vector3 moveDir = Vector3.Normalize(dir);
            float moveDist = Speed * (tick / 1000.0f);

            // 앵글 계산
            Vector3 flatDir = new Vector3(dir.X, 0, dir.Z);

            if (flatDir.LengthSquared() < 0.0001f)
                return;

            float angleRad = (float)Math.Atan2(flatDir.X, flatDir.Z);

            Quaternion targetRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, angleRad);

            RotInfo.Qx = targetRotation.X;
            RotInfo.Qy = targetRotation.Y;
            RotInfo.Qz = targetRotation.Z;
            RotInfo.Qw = targetRotation.W;

            PosInfo.PosX += moveDir.X * moveDist;
            PosInfo.PosY += moveDir.Y * moveDist;
            PosInfo.PosZ += dir.Z * moveDist; 

            BroadcastMove();
        }

        void BroadcastMove()
        {
            // 다른 플레이어한테도 알려준다
            S_Move movePacket = new S_Move();
            movePacket.ObjectId = Id;
            movePacket.PosInfo = new PositionInfo(PosInfo);
            movePacket.RotInfo = new RotationInfo(RotInfo);
            Room.Broadcast(movePacket);
        }

        protected virtual void UpdateSkill()
        {
        }

        protected virtual void UpdateDead()
        {

        }
    }
}
