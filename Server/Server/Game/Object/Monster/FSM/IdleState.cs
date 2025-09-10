using Google.Protobuf.Protocol;
using System;
using System.Diagnostics;
using System.Numerics;

namespace Server.Game.Object.Monster.FSM
{
    public class IdleState : IMonsterState
    {
        private const long SKILL_COOLDOWN_MS = 300;
        private const int SEARCH_INTERVAL_MS = 1000;
        private long _nextSearchTick = 0;

        private long _lastSkillTime = 0; // 마지막 스킬 사용 시간

        public void Enter(Monster monster)
        {
            monster.BroadcastState(CreatureState.Idle, null, null);
        }

        public void Execute(Monster monster)
        {
            if (_lastSkillTime > 0 && Environment.TickCount64 < _lastSkillTime + SKILL_COOLDOWN_MS)
                return;
            _nextSearchTick = Environment.TickCount64 + SEARCH_INTERVAL_MS;
           
            // 플레이어 판단
            Player target = monster.Room.FindPlayer(p =>
            {
                Vector3 playerPos = new Vector3(p.PosInfo.PosX, p.PosInfo.PosY, p.PosInfo.PosZ);
                Vector3 monsterPos = new Vector3(monster.PosInfo.PosX, monster.PosInfo.PosY, monster.PosInfo.PosZ);
                return monster.IsFindTargetRange(Vector3.Distance(monsterPos, playerPos));
            });

            if (target != null)
            {
                monster.Target = target;
               
                // TODO : 나중에 몬스터에 따라서 FSM을 어떻게 나눠줄 지 고민해야 함
                // 범위 안 → 바로 스킬
                if (monster.IsSkillRange())
                {
                    IMonsterState skillState = FSMManager.Instance.GetSkillState(monster.Info.MonsterType);
                    if(skillState != null)
                        monster.ChangeState(skillState);
                }
                else
                {
                    if (monster.Info.MonsterType != MonsterType.Drone)
                    {
                        IMonsterState movingState = FSMManager.Instance.GetMovingState();
                        monster.ChangeState(movingState);
                    }
                    else
                    {
                        IMonsterState skillState = FSMManager.Instance.GetSkillState(monster.Info.MonsterType);
                        if (skillState != null)
                            monster.ChangeState(skillState);
                    }
                }
            }
        }

        public void Exit(Monster monster) 
        {
            _nextSearchTick = 0;
            _lastSkillTime = 0;
        }
    }
}
