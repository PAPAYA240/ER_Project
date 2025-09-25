using Google.Protobuf.Protocol;
using Server.Data;
using System;
using System.Numerics;

namespace Server.Game
{
    // 서버 타임아웃 + 클라이언트 패킷
    public class SkillState : IMonsterState
    {
        MonsterSkillData skillData;
        private long _skillEndTime = 0; // 스킬 종료 시간
        private bool _isClientEndReceived = false; // 클라에게 종료 패킷을 받았는가?

        private Vector3 originPos;
        private Vector3 targetPos;
        public void Enter(Monster monster)
        {
            skillData = monster.Get_DecideAndUseSkill();
            if (skillData == null)
                return;

            //if(monster.Info.MonsterType == MonsterType.Alpha)
            //    DataManager.MonsterSkillDict.TryGetValue(MonsterSkill.MsAttack2, out skillData);

            _isClientEndReceived = false;
            long durationInMilliseconds = (long)(skillData.skillDuration * 1000f);
            _skillEndTime = Environment.TickCount64 + durationInMilliseconds;
            monster._delaySkillAnimationTimer = skillData.skillCoolTime;

            LookAtTarget(monster);

            monster.PushState(CreatureState.Skill, new PositionInfo(monster.PosInfo), new RotationInfo(monster.RotInfo), skillData);
        }

        //private float _delayTimer = 0;
        public void Execute(Monster monster)
        {
           // if (Environment.TickCount64 < _delayTimer)
           //    return;

            bool clientEnded = _isClientEndReceived;

            if (Environment.TickCount64 >= _skillEndTime)
                monster.ChangeState(FSMManager.Instance.GetIdleState());
        }

        private float lastSwapTime;
        bool bOrigin = true;
        private void DashLeft(Monster monster)
        {
            if (Environment.TickCount64 - lastSwapTime >= 1000)
            {
                bOrigin = !bOrigin;
                lastSwapTime = Environment.TickCount64;
            }
            else
            {
                if (Environment.TickCount64 - lastSwapTime >= 500)
                    targetPos = new Vector3(monster.PlayerTarget.PosInfo.PosX, monster.PlayerTarget.PosInfo.PosY, monster.PlayerTarget.PosInfo.PosZ);

                return;
            }

            monster.PosInfo.PosX = targetPos.X;
            monster.PosInfo.PosY = targetPos.Y;
            monster.PosInfo.PosZ = targetPos.Z;
        }

        private long _lastUpdateTime = 0;
        private void LookAtTarget(Monster monster)
        {
            Player target = monster.PlayerTarget;
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
             skillData = null;
            _skillEndTime = 0; 
            _lastUpdateTime = 0;
            _isClientEndReceived = false;

            //_delayTimer = Environment.TickCount64 + (long)(monster._delaySkillAnimationTimer * 1000f);

        }
    }
}
