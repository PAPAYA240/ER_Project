using Google.Protobuf.Protocol;
using Server.Data;
using System;
using System.Numerics;

namespace Server.Game.Object.Monster.FSM
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
            if(monster.Info.MonsterType == MonsterType.Alpha)
                DataManager.MonsterSkillDict.TryGetValue(MonsterSkill.MsSkill2, out skillData);

            _isClientEndReceived = false;
            long durationInMilliseconds = (long)(skillData.skillDuration * 1000f);
            _skillEndTime = Environment.TickCount64 + durationInMilliseconds;
            monster._delaySkillAnimationTimer = skillData.skillCoolTime;

            lastSwapTime = 0;
            originPos = new Vector3(monster.PosInfo.PosX, monster.PosInfo.PosY, monster.PosInfo.PosZ);
            targetPos = new Vector3(monster.Target.PosInfo.PosX, monster.Target.PosInfo.PosY, monster.Target.PosInfo.PosZ);
            monster.BroadcastState(CreatureState.Skill, new PositionInfo(monster.PosInfo), new RotationInfo(monster.RotInfo), skillData);
        }

        public void Execute(Monster monster)
        {
            bool clientEnded = _isClientEndReceived;

           // switch (monster.CurrentSkill)
           // {
           //     case MonsterSkill.MsSkill2:
           //         DashLeft(monster);
           //         monster.BroadcastState(CreatureState.Skill, new PositionInfo(monster.PosInfo), new RotationInfo(monster.RotInfo), skillData);
           //         break;
           // }

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
                    targetPos = new Vector3(monster.Target.PosInfo.PosX, monster.Target.PosInfo.PosY, monster.Target.PosInfo.PosZ);

                return;
            }

            monster.PosInfo.PosX = targetPos.X;
            monster.PosInfo.PosY = targetPos.Y;
            monster.PosInfo.PosZ = targetPos.Z;
          
        }

        public void Exit(Monster monster)
        {
        }
    }
}
