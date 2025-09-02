using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading;
using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game.Object.Monster
{
    public class Monster : GameObject
    {
        public Monster()
        {
            ObjectType = GameObjectType.Monster;

            // TEMP
            Stat.Level = 1;
            Stat.Hp = 100;
            Stat.MaxHp = 100;
            Stat.Speed = 5f;

            State = CreatureState.Idle;
        }

        // FSM

        public override void Update()
        {
            switch (State)
            {
                case CreatureState.Idle:
                    UpdateIdle();
                    break;
                case CreatureState.Moving:
                    UpdateMoving();
                    break;
                case CreatureState.Skill:
                    UpdateSkill();
                    break;
                case CreatureState.Dead:
                    UpdateDead();
                    break;
            }
        }

        Player _target;
        int _searchCellDist = 10;
        int _chaseCellDist = 20;

        int _skillRange = 1;
        long _nextSearchTick = 0;
        protected virtual void UpdateIdle()
        {
            if (_nextSearchTick > Environment.TickCount64)
                return;
            _nextSearchTick = Environment.TickCount64 + 1000;

            Player target = Room.FindPlayer(p =>
            {
                float dist = p.PosInfo.Distance(PosInfo);
                return dist <= _searchCellDist;
            });

            if (target == null)
                return;

            _target = target;
            State = CreatureState.Moving;
        }

        long _nextMoveTick = 0;
        protected virtual void UpdateMoving()
        {
            //if (_nextMoveTick > Environment.TickCount64)
            //    return;
            //int moveTick = (int)(1000 / Speed);
            //_nextMoveTick = Environment.TickCount64 + moveTick;

            //if (_target == null || _target.Room != Room)
            //{
            //    _target = null;
            //    State = CreatureState.Idle;
            //    BroadcastMove();
            //    return;
            //}

            //// 이동
            BroadcastMove();
        }

        void BroadcastMove()
        {
            // 다른 플레이어한테도 알려준다
            S_Move movePacket = new S_Move();
            movePacket.ObjectId = Id;
            movePacket.PosInfo = PosInfo;
            Room.Broadcast(movePacket);
        }

        long _coolTick = 0;
        protected virtual void UpdateSkill()
        {
            //if(_coolTick == 0)
            //{
            //    // 유효한 타겟인지
            //    if(_target == null || _target.Room != Room || _target.Hp == 0)
            //    {
            //        _target = null;
            //        State = CreatureState.Moving;
            //        BroadcastMove();
            //        return;
            //    }

            //    // 스킬이 아직 사용 가능한지
            //    //Vector2Int dir = _target.CellPos - CellPos;
            //    //int dist = dir.cellDisFromZero;
            //    //bool canUseSkill = (dist <= _skillRange && (dir.x == 0 || dir.y == 0));
            //    if(canUseSkill == false)
            //    {
            //        State = CreatureState.Moving;
            //        BroadcastMove();
            //        return;
            //    }

            //    // 타게팅 방향 주시
            //    MoveDir lookDir = GetDirFromVec(dir);
            //    if(Dir != lookDir)
            //    {
            //        Dir = lookDir;
            //        BroadcastMove();
            //    }

            //    Skill skillData = null;
            //    DataManager.SkillDict.TryGetValue(1, out skillData);

            //    // 데미지 판정
            //    _target.OnDamaged(this, skillData.damage + Stat.Attack);

            //    // 스킬 사용 Broadcast
            //    S_Skill skill = new S_Skill() { Info = new SkillInfo() };
            //    skill.ObjectId = Id;
            //    skill.Info.SkillId = skillData.id;
            //    Room.Broadcast(skill);

            //    // 스킬 쿨타임 적용
            //    int coolTick = (int)(1000 * skillData.cooldown);
            //    _coolTick = Environment.TickCount64 + coolTick;
            //}

            //if (_coolTick > Environment.TickCount64)
            //    return;

            //_coolTick = 0;
        }

        protected virtual void UpdateDead()
        {

        }
    }
}
