using Google.Protobuf.Protocol;
using Lucene.Net.Index;
using System;
using System.Numerics;

namespace Server.Game
{
    public class IdleState : IMonsterState
    {
        private const int SEARCH_INTERVAL_MS = 1000;
        private long _nextSearchTick = 0;
        private float _delayTimer = 0;
        //private long _lastRotationUpdateTime = 0;

        public void Enter(Monster monster)
        {
            _delayTimer = Environment.TickCount64 + (long)(monster.DelaySkillAnimationTimer * 1000f);
            _nextSearchTick = Environment.TickCount64 + SEARCH_INTERVAL_MS;

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

       // *플레이어를 타게팅 중이지만 공격 대기 중인 경우
        private void HandleAttackDelay(Monster monster)
        {
            if (monster.Info.Monster.MonsterType == MonsterType.Gamma)
            {
                //RotateTowardTarget(monster);
            }
        }

        private void ExecuteActive(Monster monster)
        {
            if (Environment.TickCount64 < _delayTimer)
            {
                HandleAttackDelay(monster);
                return;
            }
            monster.ChangeState(FSMManager.Instance.EvaluateTargetForNextState(monster));
        }
        private void ExecuteIdle(Monster monster)
        {
            if (monster.Info.Monster.MonsterType == MonsterType.Turret)
            {
                // *터렛의 경우 가까이 오면 공격
                monster.Target = monster.SearchForPlayerInRange();
                if (monster.Target != null)
                    return;
            }
            if (!monster.IsAtSpawn())
                monster.ChangeState(FSMManager.Instance.GetMovingState());
        }
    
        public void OnHit(Monster monster, Creature target) { }
        public void Exit(Monster monster)
        {
            //_lastRotationUpdateTime = 0;
            _nextSearchTick = 0;
            _delayTimer = 0;
        }
    }
}
