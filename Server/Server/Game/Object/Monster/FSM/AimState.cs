
using Google.Protobuf.Protocol;
using Server.Data;
using System;

namespace Server.Game.Object.Monster.FSM
{
    internal class AimState : IMonsterState
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

            monster.BroadcastState(CreatureState.Skill, new PositionInfo(monster.PosInfo), new RotationInfo(monster.RotInfo), skillData);
        }

        public void Execute(Monster monster)
        {
            bool timeout = Environment.TickCount64 >= _skillEndTime;
            bool clientEnded = _isClientEndReceived;

            if (timeout)
                  monster.ChangeState(new IdleState());
        }
        public void Exit(Monster monster)
        {
            monster._lastSkillTime = Environment.TickCount64;
        }
    }
}
