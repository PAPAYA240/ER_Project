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
        private ISkillBehavior _behavior;
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

            _behavior = CreateBehaviorFromClassName(_skillData.SkillBehavior);
            RotateTowardTarget(monster);

            SetupSkill(monster);

            InitializeSkillBehavior(monster);

            monster.Room.CollManager.AddHitbox(monster, _skillData.skillType);
            monster.PushState(CreatureState.Skill, new PositionInfo(monster.PosInfo), new RotationInfo(monster.RotInfo), _skillData);
        }

        public void Execute(Monster monster)
        {
            _behavior?.OnUpdate(monster);

            if (IsSkillFinished())
            {
                _behavior?.OnEnd(monster);

                if (monster.IsInSkillRange())
                    monster.ChangeState(FSMManager.Instance.GetSkillState(monster.Info.Monster.MonsterType));
                else
                    monster.ChangeState(FSMManager.Instance.GetIdleState());
            }
        }
        public void OnHit(Monster monster, Creature target)
        {
            _behavior?.OnHit(monster, target);

            if (target is Player)
            {
                Player player = target as Player;
                if (player == null) 
                    return;
                #region
                if (!_skillData.descriptionInfo.ContainsKey("Distance") ||
                    !_skillData.descriptionInfo.ContainsKey("Duration") ||
                    !_skillData.descriptionInfo.ContainsKey("Speed"))
                    return;

                StunStateDesc desc = new StunStateDesc();

                Vector3 worldRight = new Vector3(1f, 0f, 0f);
                if (_skillData.SkillBehavior == "KnockbackSkill")
                {
                    worldRight = new Vector3(-1f, 0f, 0f);
                }
                else if (_skillData.SkillBehavior == "FloatSkill")
                {
                    worldRight = new Vector3(0f, 1f, 0f);
                    desc.skillMotionType = SkillMotionType.VerticalTransform;
                }

                float distance = _skillData.descriptionInfo["Distance"];
                Vector3 rightDirection = Vector3.Transform(worldRight, monster.RotInfo.GetQuatFromRotInfo());
                Vector3 endPos = player.PosInfo.ToVector() + (rightDirection * distance);

                desc.Duration = _skillData.descriptionInfo["Duration"];
                desc.Speed = _skillData.descriptionInfo["Speed"];
                desc.EndPos = endPos;
                #endregion
                player.ChangeState(new Player_StunState(desc));
            }
        }

        public void Exit(Monster monster)
        {
            _behavior?.OnEnd(monster);
            _behavior = null;

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
            monster._delaySkillAnimationTimer = _skillData.skillCoolTime;
        }
        private void InitializeSkillBehavior(Monster monster)
        {
            _behavior = monster.CreateSkillBehavior(_skillData.SkillBehavior);
            _behavior?.OnStart(monster, _skillData);
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

        private static ISkillBehavior CreateBehaviorFromClassName(string behaviorClassName)
        {
            if (behaviorClassName == null)
                return null;

            Type behaviorType = Type.GetType(behaviorClassName);
            if (behaviorType == null)
                return null;

            try
            {
                object instance = Activator.CreateInstance(behaviorType);
                if (instance is ISkillBehavior skillBehavior)
                    return skillBehavior;
            }
            catch (Exception ex)
            {
                return null;
            }
            return null;
        }
    }
    #endregion
}