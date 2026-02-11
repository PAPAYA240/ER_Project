using Google.Protobuf.Protocol;
using Lucene.Net.Index;
using System;
using System.Numerics;

namespace Server.Game
{
    public class IdleState : IMonsterState
    {
        private float _delayTimer = 0;

        public void Enter(Monster monster)
        {
            _delayTimer = Environment.TickCount64 + (long)(monster.DelaySkillAnimationTimer * 1000f);
            monster.PushState(CreatureState.Idle, new PositionInfo(monster.PosInfo), new RotationInfo(monster.RotInfo));
        }

        public void Execute(Monster monster)
        {
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
            if (monster.Info.Monster.MonsterType == MonsterType.Turret)
            {
                monster.Target = monster.SearchForPlayerInRange();
                if (monster.Target != null)
                    return;
            }

            if (!monster.IsAtSpawn())
            {
                monster.ChangeState(FSMManager.Instance.GetMovingState());
            }
            else
            {
                if(monster.PosInfo != monster._spawnPosition)
                    monster.PushState(CreatureState.Idle, monster._spawnPosition, monster._spawnRotation);
            }
        }
    
        public void Exit(Monster monster)
        {
            _delayTimer = 0;
        }
        public void OnHit(Monster monster, Creature target) { }
    }
}
