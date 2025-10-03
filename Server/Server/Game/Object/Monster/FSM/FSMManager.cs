using Google.Protobuf.Protocol;

namespace Server.Game
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
            if (type == MonsterType.Drone || type == MonsterType.Gamma || type == MonsterType.Turret)
                return new AimState();
            return null;
        }

        public IMonsterState GetMovingState() { return new MovingState();  }
        public IMonsterState GetIdleState() { return new IdleState();  }

        // 타겟을 찾은 경우
        public IMonsterState EvaluateTargetForNextState(Monster monster)
        {
            switch (monster.Info.Monster.MonsterType)
            {
                // 근거리 몬스터
                case MonsterType.Alpha:
                case MonsterType.Omega:
                    if (monster.IsSkillRange())
                    {
                        return GetSkillState(monster.Info.Monster.MonsterType);
                    }
                    else
                    {
                        return GetMovingState();
                    }

                // 원거리 몬스터
                case MonsterType.Drone:
                case MonsterType.Gamma:
                case MonsterType.Turret:
                    return GetSkillState(monster.Info.Monster.MonsterType);

                default:
                    return GetIdleState();
            }
        }
    }
}
