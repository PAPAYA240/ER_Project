using Google.Protobuf.Protocol;
using Server.Data;
using System;
using System.Numerics;
using static Player_StunState;

namespace Server.Game
{
    public class SkillState : IMonsterState
    {
        private MonsterSkillData _skillData;
        private long _skillEndTime = 0;
        private long _lastUpdateTime = 0;

        public void Enter(Monster monster)
        {
            _skillData = monster.CastRandomSkill();
            if (_skillData == null)
            {
                monster.ChangeState(FSMManager.Instance.GetIdleState());
                return;
            }

            RotateTowardTarget(monster);

            SetupSkill(monster);

            monster.DelaySkillAnimationTimer = _skillData.skillCoolTime;

            monster.Room.CollManager.AddHitbox(monster, _skillData.skillType, new Vector2(monster.Target.PosInfo.PosX, monster.Target.PosInfo.PosZ));
            monster.PushState(CreatureState.Skill, new PositionInfo(monster.PosInfo), new RotationInfo(monster.RotInfo), _skillData);
        }

        public void Execute(Monster monster)
        {
            if (IsSkillFinished())
                 monster.ChangeState(FSMManager.Instance.GetIdleState());
        }
        public void OnHit(Monster monster, Creature target)
        {
            if (target is Player player)
            {
                if (_skillData.descriptionInfo == null)
                    return;

                bool forceSkillEffect =
                    _skillData.descriptionInfo.ContainsKey("Distance") &&
                    _skillData.descriptionInfo.ContainsKey("Duration") &&
                    _skillData.descriptionInfo.ContainsKey("Speed");

                if (!forceSkillEffect)
                    return;

                StunStateDesc desc = new StunStateDesc();
                Vector3 worldRight = new Vector3(1f, 0f, 0f);
                if (_skillData.SkillBehavior == "KnockbackSkill")
                {
                    worldRight = new Vector3(-1f, 0f, 0f);
                }
                else if (_skillData.SkillBehavior == "FloatSkill")
                {
                    worldRight = new Vector3(0f, 0f, 1f);
                }

                float distance = _skillData.descriptionInfo["Distance"];
                Vector3 rightDirection = Vector3.Transform(worldRight, monster.RotInfo.GetQuatFromRotInfo());
                Vector3 endPos = player.PosInfo.ToVector() + (rightDirection * distance);

                desc.Duration = _skillData.descriptionInfo["Duration"];
                desc.Speed = _skillData.descriptionInfo["Speed"];
                desc.EndPos = endPos;

                if(!player.IsUnstoppable() && !player.IsDead)
                    player.ChangeState(new Player_StunState(desc));
            }
        }

        public void Exit(Monster monster)
        {
            _skillData = null;
            _skillEndTime = 0;
            _lastUpdateTime = 0;
        }

        #region Private Methods
        private bool IsSkillFinished()
        {
            return Environment.TickCount64 >= _skillEndTime;
        }
        private void SetupSkill(Monster monster)
        {
            monster.CreateHitbox(_skillData.skillType);

            long durationInMilliseconds = (long)(_skillData.skillDuration * 1000f);
            _skillEndTime = Environment.TickCount64 + durationInMilliseconds;
            monster.DelaySkillAnimationTimer = _skillData.skillCoolTime;
        }
      
        private void RotateTowardTarget(Monster monster)
        {
            if (monster.Target == null)
                return;

            long currentTick = Environment.TickCount64;
            double elapsedTime = (currentTick - _lastUpdateTime) / 1000.0;
            _lastUpdateTime = currentTick;

            Vector3 targetPosition = monster.Target.PosInfo.ToVector();
            Vector3 myPosition = monster.PosInfo.ToVector();
            Vector3 direction = targetPosition - myPosition;

            monster.LookAtTarget(direction, elapsedTime, false);
        }
    }
    #endregion
}