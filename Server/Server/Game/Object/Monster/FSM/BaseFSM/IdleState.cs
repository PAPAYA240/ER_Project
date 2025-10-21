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
            _delayTimer = Environment.TickCount64 + (long)(monster._delaySkillAnimationTimer * 1000f);
            monster.PushState(CreatureState.Idle, new PositionInfo(monster.PosInfo), new RotationInfo(monster.RotInfo));
        }

        public void Execute(Monster monster)
        {
            if (Environment.TickCount64 < _nextSearchTick)
                return; 
            _nextSearchTick = Environment.TickCount64 + SEARCH_INTERVAL_MS;

            if (monster.Target != null)
                ExecuteActive(monster);

            else if (monster.Target == null)
                ExecuteIdle(monster);
        }
        private void ExecuteActive(Monster monster)
        {
            if (Environment.TickCount64 < _delayTimer)
                return;
            monster.ChangeState(FSMManager.Instance.EvaluateTargetForNextState(monster));
        }
        private void ExecuteIdle(Monster monster)
        {
            /*
             *  MonsterType type = monster.Info.Monster.MonsterType;
            switch (type)
            {
                case MonsterType.Alpha:
                case MonsterType.Omega:
                    if (!monster.IsInSkillRange())
                        monster.ChangeState(FSMManager.Instance.GetMovingState());
                    return;
            }
            monster.ChangeState(FSMManager.Instance.GetSkillState(type));
            */
            if (!monster.IsArrivalSpawn())
                monster.ChangeState(FSMManager.Instance.GetMovingState());
        }
        public void OnHit(Monster monster, Creature target) { }
        public void Exit(Monster monster) 
        {
            _nextSearchTick = 0;
            _delayTimer = 0;
        }
    }
}
