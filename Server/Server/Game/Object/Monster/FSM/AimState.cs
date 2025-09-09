
using Google.Protobuf.Protocol;
using Server.Data;
using System;
using System.Numerics;

namespace Server.Game.Object.Monster.FSM
{
    internal class AimState : IMonsterState
    {
        private long _skillEndTime = 0; // 스킬 종료 시간
        private bool _isClientEndReceived = false; // 클라에게 종료 패킷을 받았는가?
        MonsterSkillData skillData;
        public void Enter(Monster monster)
        {
            skillData = monster.Get_DecideAndUseSkill();
            if (skillData == null)
                return;
            skillData = monster.Get_DecideAndUseSkill();

            _isClientEndReceived = false;
            long durationInMilliseconds = (long)(skillData.skillDuration * 1000f);
            _skillEndTime = Environment.TickCount64 + durationInMilliseconds;

            monster.BroadcastState(CreatureState.Skill, new PositionInfo(monster.PosInfo), new RotationInfo(monster.RotInfo), skillData);
        }

        long _lastUpdateTime = 0;
        public void Execute(Monster monster)
        {
            bool timeout = Environment.TickCount64 >= _skillEndTime;
            bool clientEnded = _isClientEndReceived;
            if (timeout)
                  monster.ChangeState(new IdleState());

            long tick = Environment.TickCount64;
            double elapsedTime = (tick - _lastUpdateTime) / 1000.0;
            _lastUpdateTime = tick;

            Player target = monster.Target;
            if (target != null)
            {
                Vector3 targetPos = new Vector3(target.PosInfo.PosX, target.PosInfo.PosY, target.PosInfo.PosZ);
                Vector3 monsterPos = new Vector3(monster.PosInfo.PosX, monster.PosInfo.PosY, monster.PosInfo.PosZ);
                Vector3 dirQ = targetPos - monsterPos;
                dirQ = Vector3.Normalize(dirQ);
                Vector3 flatDir = new Vector3(dirQ.X, 0, dirQ.Z);
                flatDir = Vector3.Normalize(flatDir);

                float angleRad = (float)Math.Atan2(flatDir.X, flatDir.Z);
                Quaternion targetRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, angleRad);

                monster.RotInfo.Qx = targetRotation.X;
                monster.RotInfo.Qy = targetRotation.Y;
                monster.RotInfo.Qz = targetRotation.Z;
                monster.RotInfo.Qw = targetRotation.W;
                monster.BroadcastState(CreatureState.Skill, new PositionInfo(monster.PosInfo), new RotationInfo(monster.RotInfo), skillData);
            }
        }

        public void Exit(Monster monster)
        {
            monster._lastSkillTime = Environment.TickCount64;
        }
    }
}
