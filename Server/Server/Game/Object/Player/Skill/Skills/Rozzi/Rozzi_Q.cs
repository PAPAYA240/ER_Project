using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static Server.Data.DataUtils;

public sealed class Rozzi_Q : SkillHandlerBase
{
    public Rozzi_Q()
    {
        _animName = "SKILL_Q";
        _keyCode = KeyCode.Q;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);
        // TODO: 코스트/쿨타임 차감

        p.SendSkillConfirmPacket(ctx.Key, VariantKey.NoCollision);
    }

    public override void OnHit(Player p, SkillContext ctx)
    {
        return;
    }

    public override void OnTick(Player p, SkillContext ctx)
    {
        return;
    }

    public override void OnExit(Player p, SkillContext ctx)
    {
        base.OnExit(p, ctx);

        p.Tokens.Add(new NextInputToken
        {
            Active = true,
            RemainingUses = 1,
            ExpireUtc = TimeUtil.UtcSec() + 3.0,
            Priority = 10,
            Trigger = InputKind.Move,
            ReplacementSkillKey = "Rozzi_Q_Dash",
            CancelOnUseSkill = true
        });
    }
}

