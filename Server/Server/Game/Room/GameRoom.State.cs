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
            if (player == null || pkt == null)
                return;

            if (player.CurrentState is Player_SkillState skillState)
            {
                player.EnqueueMove(pkt);
                return;
            }

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
                if (tar != null)
                {
                    move.TargetPosition = new PositionInfo
                    {
                        PosX = tar.PosInfo.PosX,
                        PosY = tar.PosInfo.PosY,
                        PosZ = tar.PosInfo.PosZ
                    };
                }
                else
                {
                    // 안전 보강
                    move.TargetPosition = new PositionInfo
                    {
                        PosX = player.PosInfo.PosX,
                        PosY = player.PosInfo.PosY,
                        PosZ = player.PosInfo.PosZ
                    };
                }
            }

            // 4) 이미 이동 중이면 상태 유지 + 목표지만 갱신
            //    (상태 재전환/애니메이션 재생/경로 초기화 튐 방지)
            if (player.CurrentState is IReceivesMoveCommand moving && player.State == CreatureState.Moving)
            {
                moving.OnMoveCommand(player, move);
                Console.WriteLine($"HandleSetMoveTarget, OnMoveCommand / x - {move.TargetPosition.PosX}, z - {move.TargetPosition.PosZ}, State - {player.State}");
                return;
            }

            // 5) 그 외에는 새로 Moving으로 진입
            player.ChangeState(new Player_MovingState(move));
            Console.WriteLine($"HandleSetMoveTarget, ChangeState / x - {move.TargetPosition.PosX}, z - {move.TargetPosition.PosZ}");
        }

        public void HandleSkill(Player player, C_SkillInput skillPacket)
        {
            if (player == null)
                return;

            // 1) 치환할 스킬이 있는 지 확인

            // 2) 스킬이 사용 가능한 상탠지 확인

            // 3) 스펙 로드
            var key = (KeyCode)skillPacket.SkillKey;

            // 4) 컨텍스트 구성(마우스 XZ/타겟)
            var ctx = new SkillContext
            {
                MousePos = new Vector2(skillPacket.MouseX, skillPacket.MouseZ),
                TargetId = 0, // 필요하면 패킷에 포함
                Key = key,
            };

            // 5) 핸들러 결정
            ISkillHandler handler = SkillRegistry.Resolve(player.Info.Player.CharType, key);

            // 6) SkillState로 전환
            player.ChangeState(new Player_SkillState(handler, ctx));

            // 7) 클라에 허락 패킷 보내기 -> 각 Skill의 OnEnter에서
        }

        public void HandleSkillCollision(Player player, C_SkillCollisionPropose skillPacket)
        {
            if (player == null)
                return;

            // 제안 변환
            var prop = new SkillCollisionProposal
            {
                Seq = skillPacket.Seq,

                EndBlocked = new Vector3(skillPacket.EndBlockedX, player.PosInfo.PosY, skillPacket.EndBlockedZ),
                EndPass = new Vector3(skillPacket.EndPassX, player.PosInfo.PosY, skillPacket.EndPassZ),
                BehindBlocked = new Vector3(skillPacket.BehindBlockedX, player.PosInfo.PosY, skillPacket.BehindBlockedZ),

                CandidateTargetId = skillPacket.CandidateTargetId,
                //Speed = skillPacket.Speed
            };

            //if (!(player.CurrentState is Player_SkillState skillState))
            //{
            //    if (!player.PendingProposal.Has || skillPacket.Seq > player.PendingProposal.Seq)
            //    {
            //        player.PendingProposal = new PendingSkillProposal
            //        {
            //            Has = true,
            //            SkillKey = skillPacket.SkillKey,
            //            Seq = skillPacket.Seq,
            //            Prop = prop
            //        };
            //    }
            //    return;
            //}

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
        }

        //bool TryHandleMoveWithTokens(Player p, C_SetMoveTarget req)
        //{           
        //    if (p == null || req == null)
        //        return false;

        //    // 1) 유효한 토큰 고르기 (만료/잔여수 포함)
        //    var tok = p.Tokens
        //        .Where(t => t.Active
        //                    && t.Trigger == InputKind.Move
        //                    && t.RemainingUses > 0
        //                    && TimeUtil.UtcSec() <= t.ExpireUtc)
        //        .OrderByDescending(t => t.Priority)
        //        .FirstOrDefault();

        //    if (tok == null)
        //        return false;

        //    // 2) 치환 스킬 캐스트
        //    var skill = SkillRegistry.Create(tok.ReplacementSkillKey);
        //    if (skill == null)
        //        return false;

        //    var ctx = new SkillContext
        //    {
        //        Key = skill.GetKeyCode(),
        //        MousePos = new Vector2(req.TargetPos.PosX, req.TargetPos.PosZ),
        //    };

        //    if (!skill.CanCast(p, ctx))
        //        return false;

        //    p.ChangeState(new Player_SkillState(skill, ctx));

        //    // 3) 토큰 소모/비활성
        //    tok.RemainingUses--;
        //    if (tok.RemainingUses <= 0)
        //        tok.Active = false;

        //    return true;
        //}

        #region Utils
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
            if (other.ObjectType == GameObjectType.Player && me.Team == other.Team)
                return false;   

            return true; 
        }
        #endregion
    }
}
