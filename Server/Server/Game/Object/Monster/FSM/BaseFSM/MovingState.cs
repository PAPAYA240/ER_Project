using Google.Protobuf.Protocol;
using System;
using System.Numerics;

namespace Server.Game
{
    public class MovingState : IMonsterState
    {
        private const int HUNDREDS_MS = 100;
        private const int THOUSANDS_MS = 1000;

        private long _nextCalcPathTick = 0;
        private long _nextMoveTick = 0;
        private long _nextWaitTick = Environment.TickCount64;

        public void Enter(Monster monster)
        {
            // 플레이어가 없으면 스포너로 이동
            if (monster.Target == null || monster.IsReturnSpawn())
            {
                if (monster.Target != null)
                    monster.Target.Target = null;
                monster.Target = null;
                monster.Get_CalculatePath(monster.spawnPosition);
            }
            else // 플레이어를 찾아 이동
            {
                Creature player = monster.Target;
                Vector3 playerPos = new Vector3(player.PosInfo.PosX, player.PosInfo.PosY, player.PosInfo.PosZ);
                monster.Get_CalculatePath(playerPos);
            }

            _nextMoveTick = 0;
            _nextWaitTick = Environment.TickCount64;
            _nextCalcPathTick = Environment.TickCount64 + THOUSANDS_MS;
        }

        public void Execute(Monster monster)
        {
            // 스킬 시전 시
            if (monster.IsSkillRange())
            {
                if (monster._path != null) monster._path.Clear();
                IMonsterState nextState = FSMManager.Instance.EvaluateTargetForNextState(monster);
                monster.ChangeState(nextState);
                return;
            }

            if (monster.Target == null)
            {
                if (ReturnToSpawn(monster))
                    monster.ChangeState(FSMManager.Instance.GetIdleState());
                return;
            }

            if (!monster.IsFindTargetRange() || monster.Target.Room != monster.Room)
            {
                TargetNotFound(monster);
                return;
            }

            // 버벅임 방지를 위해 한 번 더 관찰
            if (_nextCalcPathTick < Environment.TickCount64)
            {
                Creature player = monster.Target;
                Vector3 playerPos = new Vector3(player.PosInfo.PosX, player.PosInfo.PosY, player.PosInfo.PosZ);
                monster.Get_CalculatePath(playerPos);
                _nextCalcPathTick = Environment.TickCount64 + THOUSANDS_MS;
            }

            if (_nextMoveTick > Environment.TickCount64) return;
            _nextMoveTick = Environment.TickCount64 + HUNDREDS_MS;

            if (monster._path != null && monster._path.Count > 0)
            {
                monster.Get_MoveAlongPath();

                monster.PushState(CreatureState.Moving, new PositionInfo(monster.PosInfo), new RotationInfo(monster.RotInfo));
            }
        }

        // 타겟을 찾지 못하는 상태라면 호출
        private void TargetNotFound(Monster monster)
        {
            // TODO : 플레이어가 가지고 있는 Monster Target을 임의로 지워주기
            if (monster.Target != null && monster.Target.Target == monster)
                monster.Target.Target = null;

            monster.Target = null;
            monster.ChangeState(FSMManager.Instance.GetIdleState());
        }


        private bool ReturnToSpawn(Monster monster)
        {
            // 도착 성공
            if (monster.IsArrivalSpawn())
                return true;

            // 스폰 장소로 돌아가기
            monster.Get_MoveAlongPath();

            monster.PushState(CreatureState.Moving, new PositionInfo(monster.PosInfo), new RotationInfo(monster.RotInfo));
            return false;
        }
        public void OnHit(Monster monster, Creature target)
        {
        }
        public void Exit(Monster monster)
        {
            _nextMoveTick = 0;
            _nextCalcPathTick = Environment.TickCount64 + THOUSANDS_MS;
        }
    }
}
