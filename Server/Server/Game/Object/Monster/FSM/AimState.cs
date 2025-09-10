
using Google.Protobuf.Protocol;
using Server.Data;
using System;
using System.Numerics;

namespace Server.Game.Object.Monster.FSM
{
    internal class AimState : IMonsterState
    {
        private MonsterSkillData skillData = null;

        private long _skillEndTime = 0; 

        private bool _isClientEndReceived = false; // 클라에게 종료 패킷을 받았는가?
        private long _lastUpdateTime = 0;


        public void Enter(Monster monster)
        {
            skillData =monster.Get_DecideAndUseSkill();
            if (skillData == null)
                return;

            _skillEndTime = Environment.TickCount64 + (long)(skillData.skillDuration * 1000f);
            monster._delaySkillAnimationTimer = skillData.skillCoolTime;
            monster.BroadcastState(CreatureState.Skill, new PositionInfo(monster.PosInfo), new RotationInfo(monster.RotInfo), skillData);
        }

        public void Execute(Monster monster)
        {
            bool timeout = Environment.TickCount64 >= _skillEndTime;
          
            if (monster.Info.MonsterType == MonsterType.Drone)
                LookAtTarget(monster);
            monster.BroadcastState(CreatureState.Skill, new PositionInfo(monster.PosInfo), new RotationInfo(monster.RotInfo), skillData);

            if (timeout)
                monster.ChangeState(FSMManager.Instance.GetIdleState());
        }

        private void LookAtTarget(Monster monster)
        {
            Player target = monster.Target;
            if (target != null)
            {
                long tick = Environment.TickCount64;
                double elapsedTime = (tick - _lastUpdateTime) / 1000.0;
                _lastUpdateTime = tick;

                Vector3 targetPos = new Vector3(target.PosInfo.PosX, target.PosInfo.PosY, target.PosInfo.PosZ);
                Vector3 monsterPos = new Vector3(monster.PosInfo.PosX, monster.PosInfo.PosY, monster.PosInfo.PosZ);
                Vector3 dirQ = targetPos - monsterPos;
                monster.LookAtTarget(dirQ, elapsedTime, false);
            }
        }

        public void Exit(Monster monster)
        {
            _skillEndTime = 0;
            _isClientEndReceived = false;
            _lastUpdateTime = 0;
        }
    }
}
