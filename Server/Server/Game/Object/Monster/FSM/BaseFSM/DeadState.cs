using Google.Protobuf.Protocol;
using Server.Data;
using System;
using static Lucene.Net.Util.AttributeSource;


namespace Server.Game
{
    public class DeadState : IMonsterState
    {
        private const int SEARCH_INTERVAL_MS = 1000;
        float _nextSearchTick = 0;
        public void Enter(Monster monster)
        {
            monster.PushState(CreatureState.Dead, new PositionInfo(monster.PosInfo), new RotationInfo(monster.RotInfo));
        }

        public void Execute(Monster monster)
        {
            if (Environment.TickCount64 < _nextSearchTick)
                return;

            _nextSearchTick = Environment.TickCount64 + SEARCH_INTERVAL_MS;
        }

        public void OnHit(Monster monster, Creature target) {}
        public void Exit(Monster monster) {}
    }
}
