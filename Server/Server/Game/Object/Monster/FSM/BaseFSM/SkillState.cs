using Google.Protobuf.Protocol;
using Server.Data;
using System;
using System.Numerics;
using System.Threading;

namespace Server.Game
{
    // 서버 타임아웃 + 클라이언트 패킷
    public class SkillState : IMonsterState
    {
        MonsterSkillData _skillData;
        private ISkillBehavior _behavior;
        private long _skillEndTime = 0; // 스킬 종료 시간
        private bool _isClientEndReceived = false; // 클라에게 종료 패킷을 받았는가?

        private Vector3 originPos;
        private Vector3 targetPos;


        public void OnHit(Monster monster, Creature target)
        {
            // 실제 스킬 로직을 담고 있는 _behavior에게 OnHit을 전달합니다.
            _behavior?.OnHit(monster, target);
        }

        public void Enter(Monster monster)
        {
            _skillData = monster.Get_DecideAndUseSkill();
            if (_skillData == null)  return;

            // Hitbox 설정
            monster.MonsterCollision(_skillData.skillType);

            _isClientEndReceived = false;
            long durationInMilliseconds = (long)(_skillData.skillDuration * 1000f);
            _skillEndTime = Environment.TickCount64 + durationInMilliseconds;
            monster._delaySkillAnimationTimer = _skillData.skillCoolTime;

            LookAtTarget(monster);

            // 스킬 설정
            _behavior = monster.CreateSkillBehavior(_skillData.SkillBehavior);
            if(_behavior != null)
                _behavior?.OnStart(monster, _skillData);

            monster.PushState(CreatureState.Skill, new PositionInfo(monster.PosInfo), new RotationInfo(monster.RotInfo), _skillData);
        }

        public void Execute(Monster monster)
        {
            _behavior?.OnUpdate(monster);

            if (Environment.TickCount64 >= _skillEndTime)
            {
                _behavior?.OnEnd(monster);
                monster.ChangeState(FSMManager.Instance.GetIdleState());
            }
        }

        private float lastSwapTime;
        bool bOrigin = true;
        private void DashLeft(Monster monster)
        {
            if (Environment.TickCount64 - lastSwapTime >= 1000)
            {
                bOrigin = !bOrigin;
                lastSwapTime = Environment.TickCount64;
            }
            else
            {
                if (Environment.TickCount64 - lastSwapTime >= 500)
                    targetPos = new Vector3(monster.Target.PosInfo.PosX, monster.Target.PosInfo.PosY, monster.Target.PosInfo.PosZ);

                return;
            }

            monster.PosInfo.PosX = targetPos.X;
            monster.PosInfo.PosY = targetPos.Y;
            monster.PosInfo.PosZ = targetPos.Z;
        }

        private long _lastUpdateTime = 0;
        private void LookAtTarget(Monster monster)
        {
            Creature target = monster.Target;
            if (target != null)
            {
                long tick = Environment.TickCount64;
                double elapsedTime = (tick - _lastUpdateTime) / 1000.0;
                _lastUpdateTime = tick;

                Vector3 targetPos = new Vector3(target.PosInfo.PosX, target.PosInfo.PosY, target.PosInfo.PosZ);
                Vector3 monsterPos = new Vector3(monster.PosInfo.PosX, monster.PosInfo.PosY, monster.PosInfo.PosZ);
                Vector3 dirQ = targetPos - monsterPos;
                monster.LookAtTarget(dirQ, elapsedTime, false);
            }
        }

        public void Exit(Monster monster)
        {
            _behavior = null;
            _skillData = null;
            _isClientEndReceived = false;
            _skillEndTime = 0;
            _lastUpdateTime = 0;

        }
    }
}
