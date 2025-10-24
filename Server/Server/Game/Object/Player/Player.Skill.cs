using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using ServerCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using static Server.Data.DataUtils;

namespace Server.Game
{
    public partial class Player : Creature
    {
        public MoveIntent Intent { get; } = new MoveIntent();
        public sealed class MoveIntent
        {
            public bool Has;
            public C_SetMoveTarget Packet;

            public void Set(C_SetMoveTarget packet) { Has = true; Packet = packet; }
            public bool TryConsume(out C_SetMoveTarget packet)
            {
                if (Has)
                { packet = Packet; Has = false; return true; }
                packet = default;
                return false;
            }
            public void Clear() { Has = false; }
        }
        public void EnqueueMove(C_SetMoveTarget packet) => Intent.Set(packet);

        public readonly List<NextInputToken> Tokens = new List<NextInputToken>();

        // 토큰 추가
        public void AddToken(NextInputToken t, double windowSec)
        {
            t.ExpireUtc = TimeUtil.UtcSec() + windowSec; // 만료시각
            t.Active = true;
            Tokens.Add(t);
            // (선택) 우선순위 높은 순으로 정렬해도 됨
            // Tokens.Sort((a,b) => b.Priority.CompareTo(a.Priority));
        }

        // 매 틱 유효한 토큰인지 검사
        public void TickTokens()
        {
            double now = TimeUtil.UtcSec();
            for (int i = Tokens.Count - 1; i >= 0; --i)
            {
                var t = Tokens[i];
                if (!t.Active || t.RemainingUses <= 0 || now > t.ExpireUtc)
                    Tokens.RemoveAt(i);
            }
        }

        public bool TryHandleMoveWithTokens(C_SetMoveTarget req)
        {
            if (req == null)
                return false;

            // 1) 유효한 토큰 고르기 (만료/잔여수 포함)
            var tok = Tokens
                .Where(t => t.Active
                            && t.Trigger == InputKind.Move
                            && t.RemainingUses > 0
                            && TimeUtil.UtcSec() <= t.ExpireUtc)
                .OrderByDescending(t => t.Priority)
                .FirstOrDefault();

            if (tok == null)
                return false;

            // 2) 치환 스킬 캐스트
            var skill = SkillRegistry.Create(tok.ReplacementSkillKey);
            if (skill == null)
                return false;

            var ctx = new SkillContext
            {
                Key = skill.GetKeyCode(),
                MousePos = new Vector2(req.TargetPos.PosX, req.TargetPos.PosZ),
            };

            if (!skill.CanCast(this, ctx))
                return false;

            ChangeState(new Player_SkillState(skill, ctx));

            // 3) 토큰 소모/비활성
            tok.RemainingUses--;
            if (tok.RemainingUses <= 0)
                tok.Active = false;

            Console.WriteLine("Token Uses");
            return true;
        }

        // 이벤트 기반 취소(스킬 시전/피격 등)
        public void CancelTokensOnSkillCast()
        {
            Tokens.RemoveAll(t => t.CancelOnUseSkill);
        }

        public void CancelTokensOnDamage()
        {
            Tokens.RemoveAll(t => t.CancelOnTakeDamage);
        }

        // (옵션) 조회 헬퍼: 특정 트리거의 최고 우선순위 토큰
        public NextInputToken PeekToken(InputKind trigger)
        {
            NextInputToken best = null;
            int bestPrio = int.MinValue;
            double now = TimeUtil.UtcSec();

            for (int i = 0; i < Tokens.Count; i++)
            {
                var t = Tokens[i];
                if (!t.Active || now > t.ExpireUtc || t.RemainingUses <= 0)
                    continue;
                if (t.Trigger != trigger)
                    continue;
                if (t.Priority > bestPrio)
                { best = t; bestPrio = t.Priority; }
            }
            return best;
        }
    }
}
