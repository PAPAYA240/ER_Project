using Google.Protobuf.Protocol;
using Server.Data;
using System;

namespace Server.Game.Object.Monster.FSM
{
    // 서버 타임아웃 + 클라이언트 패킷
    public class SkillState : IMonsterState
    {
        private long _skillEndTime = 0; // 스킬 종료 시간
        private bool _isClientEndReceived = false; // 클라에게 종료 패킷을 받았는가?
        public void Enter(Monster monster)
        {
            MonsterSkillData skillData = monster.Get_DecideAndUseSkill();
            if (skillData == null)
                return;

            _isClientEndReceived = false;
            long durationInMilliseconds = (long)(skillData.skillDuration * 1000f);
            _skillEndTime = Environment.TickCount64 + durationInMilliseconds;
            monster._delaySkillAnimationTimer = skillData.skillCoolTime;

            monster.BroadcastState(CreatureState.Skill, new PositionInfo(monster.PosInfo), new RotationInfo(monster.RotInfo), skillData);
        }

        public void Execute(Monster monster)
        {
            bool clientEnded = _isClientEndReceived;

            if (Environment.TickCount64 >= _skillEndTime)
                 monster.ChangeState(FSMManager.Instance.GetIdleState());
        }

        public void Exit(Monster monster)
        {
        }
    }
}
