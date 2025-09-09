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

            monster.BroadcastState(CreatureState.Skill, new PositionInfo(monster.PosInfo), new RotationInfo(monster.RotInfo), skillData);
            //monster.BroadcastState(CreatureState.Skill, null, null, skillData);
        }

        public void Execute(Monster monster)
        {
            bool timeout = Environment.TickCount64 >= _skillEndTime;
            bool clientEnded = _isClientEndReceived;

            if (timeout)
            {
               if (monster.Target != null && monster.Target.Room == monster.Room)
                {
                    if (monster.IsSkillRange())
                    {
                        // 아직도 범위 안 → 다시 스킬
                        monster.ChangeState(new SkillState());
                    }
                    else
                    {
                        // 범위 밖 → 이동 추격
                        monster.ChangeState(new MovingState());
                    }
                }
                else
                {
                    // 타겟이 없으면 Idle
                    monster.ChangeState(new IdleState());
                }
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
