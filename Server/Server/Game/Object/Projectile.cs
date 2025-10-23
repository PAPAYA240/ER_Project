using System;
using System.Numerics;
using Google.Protobuf.Protocol;

namespace Server.Game
{
    public class Projectile : GameObject
    {
        public Creature Owner = null;
        private long _endTime = 0;
        private long _lastTickTime = 0;
        private float _speed = 15.0f;
        public bool IsActive = false;
        public Projectile()
        {
            ObjectType = GameObjectType.Projectile;
        }

        bool bMove = false;
        public override void Update()
        {
            if (Owner == null)
                return;

            if (IsActive)
            {
                IsActive = false;
                bMove = true;
                _endTime = Environment.TickCount64 + 2000;
                _lastTickTime = Environment.TickCount64; 
            }

            if (bMove)
            {
                if (Deactivation())
                    Room.LeaveGame(Id);

                Moving();
            }
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

            Vector3 myCurPosition = Info.PosInfo.GetVector3FromPosInfo();
            Vector3 newPosition = myCurPosition + moveDistance;

            Info.PosInfo.SetPosInfoFromVector3(newPosition);
            Info.PosInfo.PosY = 1.5f;
            MovingBroadcast();
        }

      
        private bool Deactivation()
        {
            return (Environment.TickCount64 >= _endTime);
        }
        private void MovingBroadcast()
        {
            S_Move movePacket = new S_Move();
            movePacket.ObjectId = Id;
            movePacket.PosInfo = PosInfo;
            movePacket.RotInfo = RotInfo;
            Room?.Push(Room.Broadcast, movePacket);
        }
    }
}
