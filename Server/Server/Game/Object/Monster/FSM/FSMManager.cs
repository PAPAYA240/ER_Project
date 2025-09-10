using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game.Object.Monster.FSM
{
    public class FSMManager
    {
        private static FSMManager _instance = new FSMManager();
        public static FSMManager Instance { get { return _instance; } }

        private Dictionary<MonsterType, IMonsterState> _skillStates = new Dictionary<MonsterType, IMonsterState>();
        private IMonsterState _movingState;
        private IMonsterState _idleState;

        private FSMManager()
        {
            _movingState = new MovingState();
            _idleState = new IdleState();

            _skillStates.Add(MonsterType.Alpha, new SkillState());
            _skillStates.Add(MonsterType.Omega, new SkillState());
            _skillStates.Add(MonsterType.Drone, new AimState());
        }

        public IMonsterState GetSkillState(MonsterType type)
        {
            if (_skillStates.TryGetValue(type, out IMonsterState state))
                return state;
            return null;
        }

        public IMonsterState GetMovingState()
        {
            return _movingState;
        }

        public IMonsterState GetIdleState()
        {
            return _idleState;
        }
    }
}
