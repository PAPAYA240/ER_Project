using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Server.Game
{
    public class StunState : IMonsterState
    {
        private double _startTime;  
        //private Vector3 _startPos;   
    
        private MonsterStunDesc _desc;
        public class MonsterStunDesc
        {
            public float Duration;      // 기절 지속 시간
        }
        public StunState(MonsterStunDesc desc)
        {
            _desc = desc;
        }
        public void Enter(Monster monster)
        {
            _startTime = TimeUtil.UtcSec();
        }

        public void Execute(Monster monster)
        {
            double elapsedTime = TimeUtil.UtcSec() - _startTime;
            if (elapsedTime >= _desc.Duration)
            {
                monster.ChangeState(FSMManager.Instance.GetIdleState());
                return;
            }
        }

        public void Exit(Monster monster)
        {
        }

        public void OnHit(Monster monster, Creature target)
        {
        }
    }
}