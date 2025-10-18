using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Numerics;
using static Lucene.Net.Util.AttributeSource;
using static Server.Data.DataUtils;

namespace Server.Game
{
    public partial class GameRoom : Room
    {
        // 우클릭 "타겟 공격" → 즉시 AttackState
        public void HandleAttack(Player player, C_Attack pkt)
        {
            if (player == null)
                return;
            if (player.IsDead)
                return;

            player.ChangeState(new Player_AttackState(pkt.TargetId, chaseAllowed: true));
        }

        // 우클릭 유지로 들어온 이동 의도
        public void HandleSetMoveTarget(Player player, C_SetMoveTarget pkt)
        {
            if (player == null)
                return;
            if (player.IsDead)
                return;

            // 스킬 중이면: 지금 당장 이동시키지 말고 '의도'로 저장
            if (player.State == CreatureState.Skill)
            {
                var move = new C_Move();

                if (pkt.IsGround)   // TEMP : C_Move
                {
                    // 땅 지정 이동
                     move = new C_Move
                    {
                        IsTargetOn = false,
                        TargetId = 0,
                        TargetPosition = pkt.TargetPos
                    };
                }
                else
                {
                    // 타겟팅 지정 이동(그 타겟만 고수)
                    var target = player.FindTarget(pkt.TargetId);
                    if (target == null)
                        return;

                    move = new C_Move
                    {
                        IsTargetOn = true,
                        TargetId = pkt.TargetId,
                        TargetPosition = new PositionInfo
                        {
                            PosX = target.PosInfo.PosX,
                            PosY = target.PosInfo.PosY,
                            PosZ = target.PosInfo.PosZ
                        }
                    };
                }

                player.EnqueueMove(move);
                return;
            }

            if (pkt.IsGround)   // TEMP : C_Move
            {
                // 땅 지정 이동
                var move = new C_Move
                {
                    IsTargetOn = false,
                    TargetId = 0,
                    TargetPosition = pkt.TargetPos
                };

                // 이미 이동 중이면 상태 유지 + 목표지만 갱신
                if (player.CurrentState is IReceivesMoveCommand moving)
                {
                    moving.OnMoveCommand(player, move);
                    return;
                }

                player.ChangeState(new Player_MovingState(move));
            }
            else
            {
                // 타겟팅 지정 이동(그 타겟만 고수)
                var target = player.FindTarget(pkt.TargetId);
                if (target == null)
                {
                    player.ChangeState(new Player_IdleState());
                    return;
                }

                var move = new C_Move
                {
                    IsTargetOn = true,
                    TargetId = pkt.TargetId,
                    TargetPosition = new PositionInfo
                    {
                        PosX = target.PosInfo.PosX,
                        PosY = target.PosInfo.PosY,
                        PosZ = target.PosInfo.PosZ
                    }
                };

                // 이미 이동 중이면 상태 유지 + 목표지만 갱신
                if (player.CurrentState is IReceivesMoveCommand moving)
                {
                    moving.OnMoveCommand(player, move);
                    return;
                }

                player.ChangeState(new Player_MovingState(move));
            }
        }

        // S/H
        public void HandleStop(Player player, C_Stop pkt)
        {
            if (player == null)
                return;

            switch (pkt.Reason)
            {
                case StopReason.StopAll:
                    // 공격/이동 모두 중지
                    player.StopMove();
                    player.CancelAttack();
                    player.ChangeState(new Player_IdleState());
                    break;

                case StopReason.StopMoveOnly:
                    // 이동만 중지: AttackState면 추격만 금지
                    player.StopMove();
                    if (player.CurrentState is Player_AttackState atk)
                    {
                        atk.SetChaseAllowed(false);
                    }
                    else
                    {
                        player.ChangeState(new Player_IdleState());
                    }
                    break;
            }
        }

        public GameObject FindNearestEnemy(Player me, int range)
        {
            if (me == null)
                return null;

            List<int> visibleObjs = new List<int>();
            visibleObjs.AddRange(GetObjectsInRange(_players, me, range));

            float bestDistSq = range * range;
            GameObject best = null;

            foreach (var kv in visibleObjs)
            {
                var p = ObjectManager.Instance.Find(kv);
                if (!IsEnemy(me, p))
                    continue;
                float d2 = Vector3.DistanceSquared(me.Position, p.Position);
                if (d2 <= bestDistSq)
                { bestDistSq = d2; best = p; }
            }

            return best;
        }

        private bool IsEnemy(Player me, GameObject other)
        {
            if (other == null || other == me)
                return false;
            if (other.State == CreatureState.Dead)
                return false;
            if (me.Team == other.Team)
                return false;   

            return true; 
        }
    }
}
