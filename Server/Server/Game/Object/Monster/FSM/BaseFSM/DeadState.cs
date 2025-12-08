using Google.Protobuf.Protocol;
using Server.Data;
using System;


namespace Server.Game
{
    public class DeadState : IMonsterState
    {
        float _nextSearchTick = 0;
        public void Enter(Monster monster)
        {
            _nextSearchTick = Environment.TickCount64 + (DataManager.MonsterDict[monster.Info.Monster.MonsterType].deadTime * 1000);

            monster.PushState(CreatureState.Dead, new PositionInfo(monster.PosInfo), new RotationInfo(monster.RotInfo));
        }

        public void Execute(Monster monster)
        {
            if (Environment.TickCount64 < _nextSearchTick)
                return;

            monster.Room.Push(monster.Room.LeaveGame, monster.Id);
        }

        public void OnHit(Monster monster, Creature target) {}
        public void Exit(Monster monster) {}
    }
}
