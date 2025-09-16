using Google.Protobuf.Protocol;
using System;
using System.Numerics;
using System.Threading;

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
            // 플레이어가 없으면 스포너로 이동
            if (monster.PlayerTarget == null)
            {
                monster.Get_CalculatePath(monster.spawnPosition);
            }
            else // 플레이어를 찾아 이동
            {
                Player player = monster.PlayerTarget;
                Vector3 playerPos = new Vector3(player.PosInfo.PosX, player.PosInfo.PosY, player.PosInfo.PosZ);
                monster.Get_CalculatePath(playerPos);
            }

            _nextMoveTick = 0;
            _nextCalcPathTick = Environment.TickCount64 + RECALC_PATH_INTERVAL_MS;
        }

        public void Execute(Monster monster)
        {
            if (_nextMoveTick > Environment.TickCount64)
                return;
            _nextMoveTick = Environment.TickCount64 + MOVE_INTERVAL_MS;

            if (monster.PlayerTarget == null)
            {
                if(ReturnToSpawn(monster))
                    monster.ChangeState(FSMManager.Instance.GetIdleState());
                return;
            }

            // 타겟 찾는 범위를 넘어간다면 
            if (!monster.IsFindTargetRange() || monster.PlayerTarget.Room != monster.Room)
            {
                TargetNotFound(monster);
                return;
            }

            // 버벅임 방지를 위해 한 번 더 관찰
            if (_nextCalcPathTick < Environment.TickCount64)
            {
                Player player = monster.PlayerTarget;
                Vector3 playerPos = new Vector3(player.PosInfo.PosX, player.PosInfo.PosY, player.PosInfo.PosZ);
                monster.Get_CalculatePath(playerPos);

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

                IMonsterState nextState = FSMManager.Instance.EvaluateTargetForNextState(monster);
                monster.ChangeState(nextState);
                return;
            }

            // 이동만 담당하는 함수 
            monster.Get_MoveAlongPath();
            monster.BroadcastState(CreatureState.Moving, new PositionInfo(monster.PosInfo), new RotationInfo(monster.RotInfo));
        }

        // 타겟을 찾지 못하는 상태라면 호출
        private void TargetNotFound(Monster monster)
        {
            // TODO : 플레이어가 가지고 있는 Monster Target을 임의로 지워주기
            if (monster.PlayerTarget.Target == monster)
                monster.PlayerTarget.Target = null;

            monster.PlayerTarget = null;
            monster.ChangeState(FSMManager.Instance.GetIdleState());
        }

        private bool ReturnToSpawn(Monster monster)
        {
            if (monster.IsArrivalSpawn())
                return true;

            // 스폰 장소로 돌아가기
            monster.Get_MoveAlongPath();
            monster.BroadcastState(CreatureState.Moving, new PositionInfo(monster.PosInfo), new RotationInfo(monster.RotInfo));
            return false;
        }

        public void Exit(Monster monster) 
        {
            _nextMoveTick = 0;
            _nextCalcPathTick = Environment.TickCount64 + RECALC_PATH_INTERVAL_MS;
        }
    }
}
