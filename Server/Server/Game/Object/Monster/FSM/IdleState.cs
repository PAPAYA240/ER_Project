using Google.Protobuf.Protocol;
using System;
using System.Numerics;

namespace Server.Game
{
    public class IdleState : IMonsterState
    {
        private const int SEARCH_INTERVAL_MS = 1000;
        private long _nextSearchTick = 0;
        private float _delayTimer = 0;

        public void Enter(Monster monster)
        {
            // 스킬 delay를 위한 것
            _delayTimer = Environment.TickCount64 + (long)(monster._delaySkillAnimationTimer * 1000f);
            monster.PushState(CreatureState.Idle, new PositionInfo(monster.PosInfo), new RotationInfo(monster.RotInfo));
        }

        public void Execute(Monster monster)
        {
            if (Environment.TickCount64 < _nextSearchTick)
                return; 
            _nextSearchTick = Environment.TickCount64 + SEARCH_INTERVAL_MS;

            // 1. 몬스터 타겟  찾기
            if (monster.FindTarget(monster) != null)
            {
               if (Environment.TickCount64 < _delayTimer)
                  return;

                monster.ChangeState(FSMManager.Instance.EvaluateTargetForNextState(monster));
                return;
            }

            // 2. 타게팅이 없으면 스폰 자리에 있어야 함
            if (monster.PlayerTarget == null)
            {
                if (!monster.IsArrivalSpawn())
                    monster.ChangeState(FSMManager.Instance.GetMovingState()); 
                return;
            }
        }

        public void Exit(Monster monster) 
        {
            _nextSearchTick = 0;
            _delayTimer = 0;
        }
    }
}
