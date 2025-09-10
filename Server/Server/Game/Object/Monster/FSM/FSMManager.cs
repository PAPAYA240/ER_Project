using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Server.Game.Object.Monster.FSM
{
    public class FSMManager
    {
        private static FSMManager _instance = new FSMManager();
        public static FSMManager Instance { get { return _instance; } }


        private FSMManager()
        {
        }

        public IMonsterState GetSkillState(MonsterType type)
        {
            if (type == MonsterType.Alpha || type == MonsterType.Omega)
                return new SkillState();
            if (type == MonsterType.Drone || type == MonsterType.Gamma)
                return new AimState();
            return null;
        }

        public IMonsterState GetMovingState() { return new MovingState();  }
        public IMonsterState GetIdleState() { return new IdleState();  }

        // 타겟을 찾은 경우
        public IMonsterState EvaluateTargetForNextState(Monster monster)
        {
            switch (monster.Info.MonsterType)
            {
                // 근거리 몬스터
                case MonsterType.Alpha:
                case MonsterType.Omega:
                    if (monster.IsSkillRange())
                    {
                        return GetSkillState(monster.Info.MonsterType);
                    }
                    else
                    {
                        return GetMovingState();
                    }

                // 원거리 몬스터
                case MonsterType.Drone:
                case MonsterType.Gamma:
                    return GetSkillState(monster.Info.MonsterType);

                default:
                    return GetIdleState();
            }
        }
    }
}
