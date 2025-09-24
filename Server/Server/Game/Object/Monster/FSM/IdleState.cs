using Google.Protobuf.Protocol;
using System;
using System.Numerics;

namespace Server.Game.Object.Monster.FSM
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

            monster.BroadcastState(CreatureState.Idle, null, null);
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

                IMonsterState nextState = FSMManager.Instance.EvaluateTargetForNextState(monster);
                monster.ChangeState(nextState);
            }

            if (monster.PlayerTarget == null)
            {
                 if (!monster.IsArrivalSpawn())
                    monster.ChangeState(FSMManager.Instance.GetMovingState());
                return;
            }
            else
            {
                if (monster.Info.Monster.MonsterType == MonsterType.Gamma ||
                   monster.Info.Monster.MonsterType == MonsterType.Drone)
                    LookAtTarget(monster);
            }
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

                monster.BroadcastState(CreatureState.Idle, new PositionInfo(monster.PosInfo), new RotationInfo(monster.RotInfo));
            }
        }

        public void Exit(Monster monster) 
        {
            _nextSearchTick = 0;
            _delayTimer = 0;
            _lastUpdateTime = 0;
        }
    }
}
