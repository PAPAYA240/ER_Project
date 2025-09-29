using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Server.Game
{
    // 1. 움직이기 전에 움직여야 하는 목표 지점을 Get_CalculatePath(TargetPos)로 타게팅
    // 2. 실제로 움직이도록 하는 함수 Get_MoveAlongPath => 안의 FollowTarget, LookAtTarget( /* 로테이션 속도 조정 가능 */)

    public class Creature : GameObject
    {
        public Creature Target { get; set; }

        public List<Vector3> _path = new List<Vector3>();
        public int _pathIdx = 0;

        protected virtual void IdleState() { }

        #region AI
        // 1. 초반 경로 계산하는 부분
        // 움직이기 직전에 목표 위치를 전달해서 호출하면 됩니다.
        public void Get_CalculatePath(Vector3 targetPos) => CalculatePath(targetPos);
        private void CalculatePath(Vector3 targetPos)
        {
            Vector3 startPos = new Vector3(PosInfo.PosX, PosInfo.PosY, PosInfo.PosZ);
            _path = Pathfinding.FindPath(startPos, targetPos);
            _pathIdx = 0;

            if (_path != null && _path.Count > 0)
            {
                // 첫 웨이포인트가 현재 위치와 멀면 현재 위치도 경로에 넣어 자연스럽게 이동
                if (Vector3.Distance(_path[0], startPos) > 0.1f)
                    _path.Insert(0, startPos);
            }
        }

        // 2. 실제 이동 로직을 담당합니다. 움직이는 구간에 업데이트 호출하면 됩니다.
        public void Get_MoveAlongPath() => MoveAlongPath();
        private void MoveAlongPath()
        {
            if (_path == null || _path.Count == 0)
                return;

            Vector3 nextWaypoint = _path[_pathIdx];
            if (CheckArrival(nextWaypoint))
            {
                _pathIdx++;
                if (_pathIdx >= _path.Count)
                {
                    _path.Clear();

                    // 도착 시 Idle 상태로 전환 
                    // Monster의 경우는 FSM 상태를 Idle로 전환합니다
                    if(GameObjectType.Monster == ObjectType)
                        IdleState();
                }
            }

            FollowToTarget(nextWaypoint);
        }

        // 나와 Target의 위치가 얼마만큼 가까워졌느냐 
        float MOVE_STEP_INTERPOL = 0.1f;
        private bool CheckArrival(Vector3 targetPos)
        {
            Vector3 myPos = new Vector3(PosInfo.PosX, PosInfo.PosY, PosInfo.PosZ);
            float distanceToWaypoint = Vector3.Distance(myPos, targetPos);

            return distanceToWaypoint < MOVE_STEP_INTERPOL;
        }


        // 실제 이동을 담당하는 함수입니다.
        protected const float _findRange = 30.0f;
        private long _lastUpdateTime = 0;
        private const float FIXED_MOVE_STEP = 0.8f;
        public void FollowToTarget(Vector3 targetPos)
        {
            Vector3 myPos = new Vector3(PosInfo.PosX, PosInfo.PosY, PosInfo.PosZ);
            Vector3 dir = targetPos - myPos;
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
                _lastUpdateTime = tick;


            // =========== 경과 시간 계산 =========== 
            double elapsedTime = (tick - _lastUpdateTime) / 1000.0;
            _lastUpdateTime = tick;

            float distance = (float)Math.Sqrt(distanceSq);
            float moveStep = Math.Min(FIXED_MOVE_STEP, distance);

            Vector3 dirNorm = dir / distance;
            Vector3 newPos = myPos + dirNorm * moveStep;

            PosInfo.PosX = newPos.X;
            PosInfo.PosY = newPos.Y;
            PosInfo.PosZ = newPos.Z;

            // =========== 회전 =========== 
            Vector3 dirQ = new Vector3();
            if (Target != null)
            {
                Vector3 PlayerPos = new Vector3(Target.PosInfo.PosX, Target.PosInfo.PosY, Target.PosInfo.PosZ);
                float distanceToTarget = Vector3.Distance(myPos, targetPos);
                if (distanceToTarget <= _findRange)
                    dirQ = PlayerPos - myPos;
            }
            else
            {
                dirQ = targetPos - myPos;
            }
            LookAtTarget(dirQ, elapsedTime);
        }

        // 이건 회전만 담당합니다. (거리, 시간, 보간 여부, 회전 속도....)
        public void LookAtTarget(Vector3 dirQ, double elapsedTime, bool isSlerp = true, float rotationSpeed = 2.0f)
        {
            // 방향 벡터가 너무 작으면 회전하지 않음
            if (dirQ.LengthSquared() < 0.0001f)
                return;

            dirQ = Vector3.Normalize(dirQ);
            Vector3 flatDir = new Vector3(dirQ.X, 0, dirQ.Z);
            flatDir = Vector3.Normalize(flatDir);

            // 회전 각도 및 쿼터니언 계산
            float angleRad = (float)Math.Atan2(flatDir.X, flatDir.Z);
            Quaternion targetRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, angleRad);

            Quaternion newRotation;
            if (isSlerp)
            {
                // 부드러운 회전 보간
                float t = (float)Math.Clamp(rotationSpeed * elapsedTime, 0f, 1f);
                Quaternion currentRotation = new Quaternion(RotInfo.Qx, RotInfo.Qy, RotInfo.Qz, RotInfo.Qw);
                newRotation = Quaternion.Slerp(currentRotation, targetRotation, t);
            }
            else
                newRotation = targetRotation;

            // 몬스터의 회전 정보 업데이트
            RotInfo.Qx = newRotation.X;
            RotInfo.Qy = newRotation.Y;
            RotInfo.Qz = newRotation.Z;
            RotInfo.Qw = newRotation.W;
        }
    }
#endregion
}
