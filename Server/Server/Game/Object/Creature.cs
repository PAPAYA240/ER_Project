using Google.Protobuf.Protocol;
using Nito.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Server.Game
{
    public class Creature : GameObject
    {
        #region Astar Fields
        public Creature Target { get; set; }

        protected float DIST_TO_TARGET = 0.01f;
        private const float MOVE_STEP = 0.7f;

        public Deque<Node> _path = new Deque<Node>();
        public int _pathIdx = 0;

        private long _lastUpdateTime = 0;

        #endregion 

        public bool IsSkillAmplification { get; set; } = false;


        #region Projectile
        public Projectile CreateProjectile()
        {
            Projectile projectile = ObjectManager.Instance.Add<Projectile>();
            if (projectile == null)
                return null;

            projectile.Owner = this;
            projectile.Info.PosInfo = PosInfo;
            projectile.Info.RotInfo = RotInfo;
            Room?.EnterGame(projectile);

            S_Spawn spawnPacket = new S_Spawn();
            spawnPacket.Objects.Add(projectile.Info);
            Room?.Broadcast(spawnPacket);

            return projectile;
        }
        #endregion

        #region Astar
        public bool HasPath => _path != null && _path.Count > 0;
        public PathState SearchPath(Vector3 targetPos)
        {
            Vector3 startPos = PosInfo.ToVector();
            _path.Clear();

            PathState result = (PathState)(Room?.PathFind.FindPath(startPos, targetPos, ref _path));
            _pathIdx = 0;

            return result;
            //    if (_path != null && _path.Count > 0)
            //{
            //    if (Vector3.Distance(_path[0], startPos) > 0.1f)
            //        _path.Insert(0, startPos);
            //}
        }

        public void MoveAlongPath()
        {
            if (_path == null || _path.Count == 0)
                return;

            Node nextNode = _path[_pathIdx];
            Vector3 nextWaypoint = nextNode.Center;
            if (CheckArrival(nextWaypoint, (_pathIdx >= _path.Count)))
            {
                _pathIdx++;
                if (_pathIdx >= _path.Count )
                {
                    _path.Clear();
                    _pathIdx = 0;
                    return;
                }
            }

            FollowToTarget(nextWaypoint);
        }

        protected const float SKILL_RANGE = 0.01f;
        private bool CheckArrival(Vector3 targetPos, bool bLastPath)
        {
            Vector3 myPosition = PosInfo.ToVector();

            float dist = (bLastPath) ? SKILL_RANGE : DIST_TO_TARGET;
            return Vector3.Distance(myPosition, targetPos) < DIST_TO_TARGET;
        }
        public void FollowToTarget(Vector3 targetPos)
        {
            Vector3 myPosition = PosInfo.ToVector();
            Vector3 dir = targetPos - myPosition;
            float distanceSq = dir.LengthSquared();

            if (distanceSq < 0.0001f)
            {
                PosInfo.PosX = targetPos.X;
                PosInfo.PosY = targetPos.Y;
                PosInfo.PosZ = targetPos.Z;
                return;
            }

            //// =========== 이동 =========== 
            float distance = (float)Math.Sqrt(distanceSq);
            float moveStep = Math.Min(MOVE_STEP, distance);

            Vector3 dirNorm = dir / distance;
            Vector3 newPos = myPosition + dirNorm * moveStep;

            PosInfo.PosX = newPos.X;
            PosInfo.PosY = newPos.Y;
            PosInfo.PosZ = newPos.Z;

            // =========== 회전 =========== 
            Vector3 dirQ = targetPos - myPosition;

            // =========== 경과 시간 계산 =========== 
            long tick = Environment.TickCount64;
            if (_lastUpdateTime == 0)
                _lastUpdateTime = tick;
            double elapsedTime = (tick - _lastUpdateTime) / 1000.0;
            _lastUpdateTime = tick;

            LookAtTarget(dirQ, elapsedTime);
        }

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

            // TODO - 회전 보간을 여기서 할 필요가 있나?
            Quaternion newRotation;
            if (isSlerp)
            {
                float t = (float)Math.Clamp(rotationSpeed * elapsedTime, 0f, 1f);
                Quaternion currentRotation = new Quaternion(RotInfo.Qx, RotInfo.Qy, RotInfo.Qz, RotInfo.Qw);
                newRotation = Quaternion.Slerp(currentRotation, targetRotation, t);
            }
            else
                newRotation = targetRotation;

            RotInfo.Qx = newRotation.X;
            RotInfo.Qy = newRotation.Y;
            RotInfo.Qz = newRotation.Z;
            RotInfo.Qw = newRotation.W;
        }

        #endregion
    }
}
