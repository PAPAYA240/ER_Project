using Google.Protobuf.Protocol;
using J2N;
using Server.Data;
using System;
using System.Numerics;
using static System.Net.Mime.MediaTypeNames;

namespace Server.Game
{
    public class AimState : IMonsterState
    {
        private MonsterSkillData _skillData = null;
        private long _skillEndTime = 0;
        private long _lastRotationUpdateTime = 0;

        // *Skill Rotation
        private float _oscillationAngle = 0f; // 현재 진동 각도
        private float _oscillationDirection = 1f; // 진동 방향 (1 또는 -1)
        private const float MaxOscillationAngle = 25f; // 최대 15도 (좌우로 15도씩 = 총 30도)
        private const float OscillationSpeed = 20f; // 초당 30도 회전 속도 (조절 가능)
        private Vector3 _initialDirection;

        public void Enter(Monster monster)
        {
            _skillData = monster.CastRandomSkill();
            if (_skillData == null || monster.Target == null)
            {
                monster.ChangeState(FSMManager.Instance.GetIdleState());
                return;
            }

            monster.DelaySkillAnimationTimer = _skillData.skillCoolTime;

            SetupSkill(monster);

            RotateTowardTarget(monster);

            _oscillationAngle = 0f;
            _oscillationDirection = 1f;
            _lastRotationUpdateTime = Environment.TickCount64;

            // 초기 방향 저장
            if (monster.Target != null)
            {
                Vector3 targetPosition = monster.Target.PosInfo.ToVector();
                Vector3 myPosition = monster.PosInfo.ToVector();
                _initialDirection = targetPosition - myPosition;

                if (_initialDirection.LengthSquared() > 0.0001f)
                    _initialDirection = Vector3.Normalize(_initialDirection);
            }

            if (monster.Info.Monster.MonsterType == MonsterType.Turret)
            {
                float damage = monster.CalcDamage(monster, monster.Target);
                monster.Target.Room.Push(monster.Target.OnDamaged, monster, damage, false, false);
            }
            else
            {
                monster.Room.CollManager.AddHitbox(monster, _skillData.skillType, new Vector2(monster.Target.PosInfo.PosX, monster.Target.PosInfo.PosZ));
            }
            monster.PushState(CreatureState.Skill, new PositionInfo(monster.PosInfo), new RotationInfo(monster.RotInfo), _skillData);
        }

        public void Execute(Monster monster)
        {
            if (monster.CurrentSkill == MonsterSkill.MsGammaSkill2)
                RotationSkill(monster);

            if (IsSkillFinished())
            {
                if (monster.IsInSkillRange())
                {
                    MonsterType type = monster.Info.Monster.MonsterType;
                    if (type == MonsterType.Drone || type == MonsterType.Turret)
                        monster.ChangeState(FSMManager.Instance.EvaluateTargetForNextState(monster));
                    else
                    {
                        monster.ChangeState(FSMManager.Instance.GetIdleState());
                    }
                }
                else
                {
                    monster.Target = null;
                    monster.ChangeState(FSMManager.Instance.GetIdleState());
                }
            }
        }

        public void OnHit(Monster monster, Creature target) { }
        public void Exit(Monster monster)
        {
            _skillEndTime = 0;
            _lastRotationUpdateTime = 0;
            _oscillationAngle = 0f;
            _oscillationDirection = 1f;
        }
     

        #region Private Methods
        private void RotationSkill(Monster monster)
        {
            if (monster.Target == null)
                return;

            long currentTick = Environment.TickCount64;
            double elapsedTime = (currentTick - _lastRotationUpdateTime) / 1000.0;

            // elapsedTime이 너무 크면 제한 (첫 프레임 보호)
            if (elapsedTime > 0.1)
                elapsedTime = 0.016; // 약 60fps 기준

            _lastRotationUpdateTime = currentTick;

            // 진동 각도 업데이트
            float angleChange = OscillationSpeed * (float)elapsedTime * _oscillationDirection;
            _oscillationAngle += angleChange;

            // 최대 각도에 도달하면 방향 전환
            if (_oscillationAngle >= MaxOscillationAngle)
            {
                _oscillationAngle = MaxOscillationAngle;
                _oscillationDirection = -1f;
            }
            else if (_oscillationAngle <= -MaxOscillationAngle)
            {
                _oscillationAngle = -MaxOscillationAngle;
                _oscillationDirection = 1f;
            }

            // Y축 기준으로 진동 각도만큼 회전
            float radians = _oscillationAngle * (MathF.PI / 180f);
            Quaternion oscillationRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, radians);
            Vector3 oscillatedDirection = Vector3.Transform(_initialDirection, oscillationRotation);

            // 회전 적용 (부드러운 회전을 위해 slerp 사용)
            monster.LookAtTarget(oscillatedDirection, elapsedTime, true, 10f);
            monster.PushState(CreatureState.Skill, new PositionInfo(monster.PosInfo), new RotationInfo(monster.RotInfo), null, false);
        }
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

            monster.DelaySkillAnimationTimer = _skillData.skillCoolTime;
        }
        #endregion
    }
}