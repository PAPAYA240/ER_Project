using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

public sealed class Rozzi_D : SkillHandlerBase
{
    public override void OnEnter(Player p, SkillSpec spec, SkillContext ctx)
    {
        base.OnEnter(p, spec, ctx);
        p.SendAnimPacket("ROZZI_D", 0.05f);
    }

    public override void OnHit(Player p, SkillSpec spec, SkillContext ctx)
    {
        return;
    }

    public override void OnTick(Player p, SkillSpec spec, SkillContext ctx)
    {
        return;
    }

    public override void OnExit(Player p, SkillSpec spec, SkillContext ctx)
    {
        base.OnExit(p, spec, ctx);
    }
}

