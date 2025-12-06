using Google.Protobuf.Protocol;
using System;
using System.Numerics;

namespace Server.Game
{
    public class MovingState : IMonsterState
    {
        private const int MOVE_INTERVAL_MS = 100;
        private const int PATH_RECALC_INTERVAL_MS = 2000;

        private long _nextCalcPathTick = 0;
        private long _nextMoveTick = 0;

        public void Enter(Monster monster)
        {
            _nextMoveTick = 0;
            _nextCalcPathTick = Environment.TickCount64 + PATH_RECALC_INTERVAL_MS;

            CalculateInitPath(monster);
            monster.PushState(CreatureState.Moving, new PositionInfo(monster.PosInfo), new RotationInfo(monster.RotInfo));
        }

        public void Execute(Monster monster)
        {
            // 1. 타겟 유효성 검증
            if (!ValidateTarget(monster))
                return;

            // 2. 스폰 복귀 체크
            CheckReturnToSpawn(monster);

            // 3. 스킬 사용 가능 체크
            if (!monster.ReturnToSpawn && TryTransitionToSkill(monster))
                return;

            // 4. 이동 처리
            if (TryMove(monster))
            {
                monster.PushState(
                    CreatureState.Moving,
                    new PositionInfo(monster.PosInfo),
                    new RotationInfo(monster.RotInfo)
                );
            }
            else if (!monster.HasPath)
            {
                if (monster.ReturnToSpawn)
                {
                    monster.ReturnToSpawn = false;
                }

                RecalculatePath(monster);
            }
        }
        public void OnHit(Monster monster, Creature target) { }
        public void Exit(Monster monster)
        {
            _nextMoveTick = 0;
            _nextCalcPathTick = Environment.TickCount64 + PATH_RECALC_INTERVAL_MS;
        }

        #region Private Methods
        private bool ValidateTarget(Monster monster)
        {
            if (monster.Target == null)
                return true;

            if (monster.Room != monster.Target.Room)
            {
                monster.Target = null;
                monster.ChangeState(FSMManager.Instance.GetIdleState());
                return false;
            }
            return true;
        }
        private void CalculateInitPath(Monster monster)
        {
            if (monster.ReturnToSpawn)
                monster.SearchPath(monster._spawnPosition);

            else if (monster.Target is Creature target)
            {
                Vector3 targetPos = target.PosInfo.ToVector();
                monster.SearchPath(targetPos);
            }
        }
        private void CheckReturnToSpawn(Monster monster)
        {
            if (monster.ReturnToSpawn)
                return;

            if (monster.IsReturnSpawn())
            {
                monster.Target = null;
                monster.ReturnToSpawn = true;
                monster.ChangeState(FSMManager.Instance.GetMovingState());
            }
        }

        private bool TryTransitionToSkill(Monster monster)
        {
            if (!monster.IsInSkillRange())
                return false;

            monster._path?.Clear();
            monster.ChangeState(FSMManager.Instance.EvaluateTargetForNextState(monster));
            return true;
        }
        private bool TryMove(Monster monster)
        {
            RecalculatePath(monster);

            if (Environment.TickCount64 < _nextMoveTick)
                return false;
            _nextMoveTick = Environment.TickCount64 + MOVE_INTERVAL_MS;

            if (monster._path != null && monster._path.Count > 0)
            {
                monster.MoveAlongPath();
                return true;
            }
            return false;
        }
        private void RecalculatePath(Monster monster)
        {
            if (monster.Target != null)
            {
                Vector3 currentTargetPos = new Vector3(
                    monster.Target.PosInfo.PosX,
                    monster.Target.PosInfo.PosY,
                    monster.Target.PosInfo.PosZ
                );

                if (monster._lastTargetPos != Vector3.Zero)
                {
                    float movedDistance = Vector3.Distance(monster._lastTargetPos, currentTargetPos);
                    if (movedDistance < 3f) // 3m 이하로 움직였으면 재계산 안 함
                        return;
                }

                monster._lastTargetPos = currentTargetPos;
                monster.SearchPath(currentTargetPos);
            }
        }
        #endregion
    }
}
