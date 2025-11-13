using Google.Protobuf.Protocol;
using Server.Data;
using System;
using System.Numerics;

namespace Server.Game
{
    public class AimState : IMonsterState
    {
        private MonsterSkillData _skillData = null;
        private long _skillEndTime = 0;
        private long _lastRotationUpdateTime = 0;

        public void Enter(Monster monster)
        {
            _skillData = monster.CastRandomSkill();
            if (_skillData == null)
            {
                monster.ChangeState(FSMManager.Instance.GetIdleState());
                return;
            }

            SetupSkill(monster);

            RotateTowardTarget(monster);
            monster.Room.CollManager.AddHitbox(monster, _skillData.skillType);

            monster.PushState(CreatureState.Skill, new PositionInfo(monster.PosInfo), new RotationInfo(monster.RotInfo), _skillData);
        }

        public void Execute(Monster monster)
        {
            if (IsSkillFinished())
            {
                if (monster.IsInSkillRange())
                    monster.ChangeState(FSMManager.Instance.EvaluateTargetForNextState(monster));
                else
                    monster.ChangeState(FSMManager.Instance.GetIdleState());
            }
        }

        public void OnHit(Monster monster, Creature target) { }
        public void Exit(Monster monster)
        {
            _skillEndTime = 0;
            _lastRotationUpdateTime = 0;
        }

        #region Private Methods
        private bool IsSkillFinished()
        {
            return Environment.TickCount64 >= _skillEndTime;
        }
        private void RotateTowardTarget(Monster monster)
        {
            if (monster.Target == null)
                return;

            long currentTick = Environment.TickCount64;
            double elapsedTime = (currentTick - _lastRotationUpdateTime) / 1000.0;
            _lastRotationUpdateTime = currentTick;

            Vector3 targetPosition = monster.Target.PosInfo.ToVector();
            Vector3 myPosition = monster.PosInfo.ToVector();
            Vector3 direction = targetPosition - myPosition;

            monster.LookAtTarget(direction, elapsedTime, false);
        }

        private void SetupSkill(Monster monster)
        {
            // 스킬 지속 시간 설정
            long durationMs = (long)(_skillData.skillDuration * 1000f);
            _skillEndTime = Environment.TickCount64 + durationMs;

            monster._delaySkillAnimationTimer = _skillData.skillCoolTime;
        }
        #endregion
    }
}