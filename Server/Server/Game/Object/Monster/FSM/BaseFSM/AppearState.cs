using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game
{
    public class AppearState : IMonsterState
    {
        private float _stateTimer = 0f;
        private float _endTime = 0f;
        public void Enter(Monster monster)
        {
            _stateTimer = Environment.TickCount64;
            _endTime = Environment.TickCount64 + DataManager.MonsterDict[monster.Info.Monster.MonsterType].appearTime; 
            monster.PushState(CreatureState.Appear, new PositionInfo(monster.PosInfo), new RotationInfo(monster.RotInfo));
        }

        public void Execute(Monster monster)
        {
            _stateTimer += Environment.TickCount64;
            if (_stateTimer >= _endTime)
                monster.ChangeState(new IdleState()); 
        }

        public void OnHit(Monster monster, Creature target) {}
        public void Exit(Monster monster) { }
    }
}