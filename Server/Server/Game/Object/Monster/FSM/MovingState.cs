using Google.Protobuf.Protocol;
using System;

namespace Server.Game.Object.Monster.FSM
{
    public class MovingState : IMonsterState
    {
        private const int MOVE_INTERVAL_MS = 100;
        private const int RECALC_PATH_INTERVAL_MS = 1000;
        private long _nextCalcPathTick = 0;
        private long _nextMoveTick = 0;

        public void Enter(Monster monster)
        {
            monster.Get_CalculatePath();

            _nextMoveTick = 0;
            _nextCalcPathTick = Environment.TickCount64 + RECALC_PATH_INTERVAL_MS;
        }

        public void Execute(Monster monster)
        {
            if (_nextMoveTick > Environment.TickCount64)
                return;
            _nextMoveTick = Environment.TickCount64 + MOVE_INTERVAL_MS;

            if (monster.Target == null || monster.Target.Room != monster.Room)
            {
                monster.ChangeState(FSMManager.Instance.GetIdleState());
                return;
            }

            // 버벅임 방지를 위해 한 번 더 관찰
            if (_nextCalcPathTick < Environment.TickCount64)
            {
                monster.Get_CalculatePath();
                _nextCalcPathTick = Environment.TickCount64 + RECALC_PATH_INTERVAL_MS;
            }

            if (monster._path == null || monster._path.Count == 0)
            {
                monster.ChangeState(FSMManager.Instance.GetIdleState());
                return;
            }

            if (monster.IsSkillRange())
            {
                monster._path.Clear();
                monster.ChangeState(FSMManager.Instance.GetSkillState(monster.Info.MonsterType));
                return;
            }

            // 이동만 담당하는 함수 
            monster.Get_MoveAlongPath();
            monster.BroadcastState(CreatureState.Moving, new PositionInfo(monster.PosInfo), new RotationInfo(monster.RotInfo));
        }
        public void Exit(Monster monster) 
        {
            _nextMoveTick = 0;
            _nextCalcPathTick = Environment.TickCount64 + RECALC_PATH_INTERVAL_MS;
        }
    }
}
