using Google.Protobuf.Protocol;
using System;
using System.Numerics;

namespace Server.Game.Object.Monster.FSM
{
    public class IdleState : IMonsterState
    {
        private long _nextSearchTick = 0;
        private const int SEARCH_INTERVAL_MS = 1000;

        private const long SKILL_COOLDOWN_MS = 300;
        private long _lastSkillTime = 0; // 마지막 스킬 사용 시간
        private float _searchCellDist = 1000.0f; // 탐색 거리

        public void Enter(Monster monster)
        {
            monster.BroadcastState(CreatureState.Idle, null, null);
        }

        public void Execute(Monster monster)
        {
            if (_lastSkillTime > 0 && Environment.TickCount64 < _lastSkillTime + SKILL_COOLDOWN_MS)
                return;
            _nextSearchTick = Environment.TickCount64 + SEARCH_INTERVAL_MS;
            if (_lastSkillTime > 0 && Environment.TickCount64 < _lastSkillTime + SKILL_COOLDOWN_MS)
                return;

            // 플레이어 판단
            Player target = monster.Room.FindPlayer(p =>
            {
                Vector3 playerPos = new Vector3(p.PosInfo.PosX, p.PosInfo.PosY, p.PosInfo.PosZ);
                Vector3 monsterPos = new Vector3(monster.PosInfo.PosX, monster.PosInfo.PosY, monster.PosInfo.PosZ);
                return Vector3.Distance(monsterPos, playerPos) <= _searchCellDist;
            });

            if (target != null)
            {
                monster.Target = target;
                monster._lastPlayerPosition = new Vector3(
                    target.PosInfo.PosX,
                    target.PosInfo.PosY,
                    target.PosInfo.PosZ
                );

                // TODO : 나중에 몬스터에 따라서 FSM을 어떻게 나눠줄 지 고민해야 함
                // 범위 안 → 바로 스킬
                if (monster.IsSkillRange())
                {
                    monster.ChangeState(monster.GetSkillState());
                }
                else
                {
                    // 범위 밖 → 추격
                    if(monster.Info.MonsterType != MonsterType.Drone)
                        monster.ChangeState(new MovingState());
                    else
                        monster.ChangeState(monster.GetSkillState());
                }
            }
        }

        public void Exit(Monster monster)
        {
        }
    }
}
