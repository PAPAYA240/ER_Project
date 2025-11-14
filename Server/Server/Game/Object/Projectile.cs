using System;
using System.Numerics;
using Google.Protobuf.Protocol;

namespace Server.Game
{
    public class Projectile : GameObject
    {
        public Creature Owner = null;
        public ProjectileType ProjectileType { get; set; }
        private long _endTime = 0;
        private long _lastTickTime = 0;
        private float _speed = 15.0f;

        public Projectile()
        {
            ObjectType = GameObjectType.Projectile;
        }
        
        public virtual void Init()
        {
            if (Owner == null)
                return;

            _endTime = Environment.TickCount64 + 500;
            _lastTickTime = Environment.TickCount64;

            // Owner의 현재 위치를 복사
            Info.PosInfo = new PositionInfo
            {
                PosX = Owner.PosInfo.PosX,
                PosY = Owner.PosInfo.PosY,
                PosZ = Owner.PosInfo.PosZ
            };

            Info.RotInfo = Owner.RotInfo; 
        }

        public override void Update()
        {
            if (Owner == null)
                return;

            if (Deactivation())
            {
                Room.LeaveGame(Id);
                return;
            }
            Moving();
        }

        private void Moving()
        {
            // 플레이어 위치 계산
            long currentTickTime = Environment.TickCount64;
            long deltaMilliseconds = currentTickTime - _lastTickTime;
            float deltaTimeSeconds = deltaMilliseconds / 1000.0f;

            _lastTickTime = currentTickTime;

            Vector3 forwardVector = Info.RotInfo.Forward();
            Vector3 moveDistance = forwardVector * _speed * (float)deltaTimeSeconds;

            Vector3 myCurPosition = Info.PosInfo.ToVector();
            Vector3 newPosition = myCurPosition + moveDistance;

            Info.PosInfo.SetPosInfoFromVector3(newPosition);
            Info.PosInfo.PosY = 1.5f;
            //MovingBroadcast();
            SendMovePacket(PosInfo, RotInfo);

            Console.WriteLine($"@ Moving - {PosInfo.PosX}, {PosInfo.PosZ}");
        }

        protected virtual bool Deactivation()
        {
            // 경과 시간 or 충돌을 했을 경우에 비활성화
            return (Environment.TickCount64 >= _endTime);
        }

        //protected void MovingBroadcast()
        //{
        //    S_Move packet = new S_Move
        //    {
        //        ObjectId = base.Id,
        //        PosInfo = PosInfo,
        //        RotInfo = RotInfo
        //    };
        //    base.Room?.Push(Room.Broadcast, packet);
        //}
    }
}
