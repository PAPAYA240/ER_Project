using Google.Protobuf.Protocol;
using System;

namespace Server.Game.Object.Monster.FSM
{
    public class MovingState : IMonsterState
    {
        private long _nextMoveTick = 0;
        private const int MOVE_INTERVAL_MS = 100;
        private const int RECALC_PATH_INTERVAL_MS = 1000;
        private long _nextCalcPathTick = 0;

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
                monster.ChangeState(new IdleState());
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
                Console.WriteLine("문제1");
                monster.ChangeState(new IdleState());
                return;
            }

            if (monster.IsSkillRange())
            {
                monster._path.Clear();
                Console.WriteLine("스킬1");
                monster.ChangeState(new SkillState());
                return;
            }

            // 어디로 움직여야 하는 지 계산
            //Vector3 targetPos = new Vector3(monster.Target.PosInfo.PosX, monster.Target.PosInfo.PosY, monster.Target.PosInfo.PosZ);
            //Player _target = monster.Target;
            //if (Vector3.Distance(monster._lastPlayerPosition, targetPos) > 0.5f)
            //{
            //    monster.Get_CalculatePath();
            //    monster._lastPlayerPosition = targetPos;
            //}

            // 이동만 담당하는 함수 
            monster.Get_MoveAlongPath();
            monster.BroadcastState(CreatureState.Moving, new PositionInfo(monster.PosInfo), new RotationInfo(monster.RotInfo));
        }
        public void Exit(Monster monster) { }
    }
}
