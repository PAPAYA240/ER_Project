using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Numerics;
using static Lucene.Net.Index.SegmentReader;
using static Lucene.Net.Util.AttributeSource;
using static Server.Data.DataUtils;
using static Server.Game.Player;
using Server.Game;

namespace Server.Game
{
    public partial class GameRoom : Room
    {
        float STOP_BUFFER = 0.1f;

        // 우클릭 "타겟 공격" → 즉시 AttackState
        public void HandleAttack(Player player, C_Attack pkt)
        {
            if (player == null)
                return;
            if (player.IsDead)
                return;

            // Attack 상태이고 공격 애니메이션이 진행중이면 
            if (player.CurrentState is IReceivesAttackCommand swing && player.State == CreatureState.Attack)
            {
                // 애니메이션 끝난 뒤 타겟이 변경됨
                swing.RequestTargetChange(pkt.TargetId);
                return;
            }
            // Skill 상태이지만 끊을 수 없는 상태라면 return
            else if(player.CurrentState is Player_SkillState skill && !skill.CanStopSkill)
            {
                return;
            }

            player.ChangeState(Player_AttackState.CreateAttackState(player, pkt.TargetId, chaseAllowed: true));
        }

        // 우클릭 유지로 들어온 이동 의도
        public void HandleSetMoveTarget(Player player, C_SetMoveTarget pkt)
        {
            if (player == null || pkt == null)
                return;

            // 0) 플레이어가 이동할 수 있는 상태인지 확인
            if (!player.CanMove())
                return;

            // 1) 타겟 검증 → 지형 이동 정규화
            if (!pkt.IsGround)
            {
                GameObject tar = player.FindTarget(pkt.TargetId);
                bool attackable = (tar != null) && IsEnemy(player, tar) && tar.State != CreatureState.Dead;
                if (!attackable)
                {
                    pkt.IsGround = true;
                    pkt.TargetId = 0;
                    if (pkt.TargetPos == null)
                    {
                        pkt.TargetPos = new PositionInfo
                        {
                            PosX = player.PosInfo.PosX,
                            PosY = player.PosInfo.PosY,
                            PosZ = player.PosInfo.PosZ
                        };
                    }
                }
            }

            // 2) 치환 토큰: post-move Enqueue + 스킬 캐스트되면 종료
            if (player.TryHandleMoveWithTokens(pkt))
                return;

            // 3) pkt → C_Move 정규화
            var move = new C_Move();

            if (pkt.IsGround)
            {
                move.IsTargetOn = false;
                move.TargetId = 0;
                move.TargetPosition = pkt.TargetPos ?? new PositionInfo
                {
                    PosX = player.PosInfo.PosX,
                    PosY = player.PosInfo.PosY,
                    PosZ = player.PosInfo.PosZ
                };
            }
            else
            {
                move.IsTargetOn = true;
                move.TargetId = pkt.TargetId;

                var tar = player.FindTarget(pkt.TargetId);
                if (pkt.TargetPos != null)
                    move.TargetPosition = new PositionInfo(pkt.TargetPos);
                else if (tar != null)
                    move.TargetPosition = new PositionInfo(tar.PosInfo);
                else
                    move.TargetPosition = new PositionInfo(player.PosInfo);
            }

            // 4) 먼저 공격 가능한지 부터 확인
            GameObject target = null;
            if (!pkt.IsGround)
                target = player.FindTarget(pkt.TargetId);

            if (!pkt.IsGround && target != null)
            {
                float dist = Vector3.Distance(player.Position, target.Position);
                float effectiveRange = player.AttackRange + STOP_BUFFER;

                if (dist <= effectiveRange)
                {
                    // 이미 공격 상태면: 타겟만 변경 or 무시
                    if (player.CurrentState is Player_AttackState attack)
                    {
                        if (attack._targetId != pkt.TargetId)
                            attack.ChangeTarget(pkt.TargetId);
                    }
                    else
                        player.ChangeState(Player_AttackState.CreateAttackState(player, pkt.TargetId, chaseAllowed: true));

                    return;
                }
            }

            // 5) 아직 사거리 밖이면, 그때 이동 상태로 처리
            if (player.CurrentState is IReceivesMoveCommand moving)
                // 이미 MovingState(또는 이동 가능한 다른 상태)라면 목적지만 갱신
                moving.OnMoveCommand(player, move);
            else
                player.ChangeState(new Player_MovingState(move));      
        }

        public void HandleSkill(Player player, C_SkillInput skillPacket)
        {
            if (player == null)
                return;

            player.Skill.HandleSkillPacket(skillPacket);
        }

        public void HandleExecuteSkill(Player player, C_SkillExecute skillPacket)
        {
            var key = (KeyCode)skillPacket.SkillKey;
            if (player.Info.Player.CharType == CharacterType.Theodore)
            {
                Player_SkillState skillstate = player.CurrentState as Player_SkillState;
                skillstate.Ctx.MousePos = new Vector2(skillPacket.MousePosX, skillPacket.MousePosZ);

                if (player.CurrentState is Player_SkillState skillState)
                {
                    skillState.Handler.OnAttack(player);
                }
            }
        }

        public void HandlerPrepareSkill(Player player, C_SkillPrepare skillPacket)
        {
            var key = (KeyCode)skillPacket.SkillKey;

            if (!player.CanUseSkill(key))
                return;

            ISkill handler = SkillRegistry.Prepare(player.Info.Player.CharType, key);
            var ctx = new SkillContext
            {
                Key = key,
            };

            player.ChangeState(new Player_SkillState(handler, ctx));
        }

        public void HandlerChargeCancelSkill(Player player, C_SkillCancel skillPacket)
        {
            if (player.CurrentState is IReceivesStopCommand stop)
                stop.OnStopCommand(player, null);
        }

        public void HandleSkillCollision(Player player, C_SkillCollisionPropose skillPacket)
        {
            if (player == null)
                return;

            // 제안 변환
            var prop = new SkillCollisionProposal
            {
                requestId = skillPacket.RequestId,
                Seq = skillPacket.Seq,

                collisionPos = new Vector3(skillPacket.CollisionX, player.PosInfo.PosY, skillPacket.CollisionZ),
                //Speed = skillPacket.Speed
            };

            // 스킬로 전달
            if (player.CurrentState is Player_SkillState skillState)
                skillState.Handler.OnPropose(player, prop); 
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

            if (player.CurrentState is IReceivesStopCommand stop)
                stop.OnStopCommand(player, pkt);
        }

        public void HandleChargingSkill(Player p, C_ChargingSkill packet)
        {
            p.ChargingRatio = packet.CharginRatio;

            Player_SkillState state = p.CurrentState as Player_SkillState;
            if (state != null)
            {
                ChargingSkillHandler skillHandler = state.Handler as ChargingSkillHandler;

                if(skillHandler != null)
                {
                    skillHandler.OnCharge(p, state.Ctx);
                }
            }
        }

        public void HandleDeployingLoop(Player player, C_DeployingLoop pkt)
        {
            if (player == null)
                return;
            if (player.IsDead)
                return;

            if (!(player.State == CreatureState.Idle || player.State == CreatureState.Moving))
                return;

            //if (CurPhase < 1 /*2*/)
            //    return;

            player.ChangeState(new Player_TeleportState(pkt.IoPos));
        }

        public void HandleRozziNormalAttack(Player player, C_RozziNormalAttack pkt)
        {
            if (player == null || player.IsDead)
                return;

            if(player.CurrentState is Rozzi_AttackState attackState)
                attackState.ApplyProjectileHit(player, pkt);
            else
            {
                Rozzi_AttackState state = new Rozzi_AttackState(pkt.TargetId);
                state.ApplyProjectileHit(player, pkt);
            }
        }

        public void HandleAttackTargetInvalid(Player player, C_AttackTargetInvalid pkt)
        {
            if (player == null || player.IsDead)
                return;

            //InvalidTargetReason reason = pkt.Reason;
            player.ChangeState(new Player_IdleState());
        }

        public void HandleBaseTrigger(Player player, C_BaseTrigger pkt)
        {
            if (player == null || player.IsDead)
                return;

            if (pkt.Team != player.Team)
                return;

            if (pkt.IsInside)
                player.AddStatEffect(IRegenEffect.StatRegenType.BaseAreaRegen);
            else
                player.RemoveStatEffect(IRegenEffect.StatRegenType.BaseAreaRegen);
        }

        #region Utils
        public GameObject FindNearestEnemy(Player me, int range)
        {
            if (me == null)
                return null;

            // 정현 오빠 도와줘
            List<int> visibleObjs = new List<int>();
            //visibleObjs.AddRange(GetObjectsInRange(_players, me, range));

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
            if (other.ObjectType == GameObjectType.Player && me.Team == other.Team)
                return false;   

            return true; 
        }
        #endregion
    }
}
