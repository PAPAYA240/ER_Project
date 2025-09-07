using Google.Protobuf.Protocol;
using Server.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game.Object.Monster.FSM
{
    // 서버 타임아웃 + 클라이언트 패킷
    public class SkillState : IMonsterState
    {
        private long _skillEndTime = 0; // 스킬 종료 시간
        private bool _isClientEndReceived = false; // 클라에게 종료 패킷을 받았는가?
        public void Enter(Monster monster)
        {
           Console.WriteLine("2. 스킬");
            MonsterSkillData skillData = monster.Get_DecideAndUseSkill();
            if (skillData == null)
                return;

            _isClientEndReceived = false;
            long durationInMilliseconds = (long)(skillData.skillDuration * 1000f);
            _skillEndTime = Environment.TickCount64 + durationInMilliseconds;

            monster.BroadcastState(CreatureState.Skill, null, null, skillData);
        }

        public void Execute(Monster monster)
        {
            bool timeout = Environment.TickCount64 >= _skillEndTime;

            bool clientEnded = _isClientEndReceived;

            if (timeout)
            {
                Console.WriteLine("2. 타임아웃");
                monster.ChangeState(new IdleState()); 
            }
        }

        public void Exit(Monster monster)
        {
            monster._lastSkillTime = Environment.TickCount64;
        }
        public void SetClientEndReceived()
        {
            _isClientEndReceived = true;
        }
    }
}
