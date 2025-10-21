using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

public sealed class Rozzi_Q_Dash : SkillHandlerBase
{
    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);
        p.SendAnimPacket("SKILL_Q_Dash", 0.05f);
        p.SendSkillConfirmPacket(ctx.Key, VariantKey.Followup);
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
    }
}

