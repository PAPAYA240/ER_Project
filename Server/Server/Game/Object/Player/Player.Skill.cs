using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using ServerCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;
using static Server.Data.DataUtils;

namespace Server.Game
{
    public partial class Player : Creature
    {
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
