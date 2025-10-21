using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

public sealed class Skill_Blink : SkillHandlerBase
{
    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);

        p.SendSkillConfirmPacket(ctx.Key, VariantKey.Cast);
    }

    public override void OnHit(Player p, SkillContext ctx)
    {
        return;
    }

    public override void OnTick(Player p, SkillContext ctx)
    {
        if (_committed)
            return;

        if (!TryConsumeLatest(out var prop))
            return;

        var from = new Vector3(p.PosInfo.PosX, p.PosInfo.PosY, p.PosInfo.PosZ);
        var end = prop.EndPass;

        CommitMotionOnce(p, from, end);

        _committed = true;
    }

    public override void OnExit(Player p, SkillContext ctx)
    {
        base.OnExit(p, ctx);
    }

    private void CommitMotionOnce(Player p, Vector3 from, Vector3 end)
    {
        _finalEnd = end;

        float dist = Vector3.Distance(from, end);
        float duration = 0;

        p.SendSkillMotion(
             type: SkillMotionType.Blink,
             start: from,
             end: _finalEnd,
             duration: duration,
             anim: ""/*spec.AnimName*/,
             curveId: "EaseOutCubic",
             serverCollision: true,
             authoritativeEnd: true);

        p.Flags.IsInSkillMotion = true;

        //p.PosInfo = new PositionInfo { PosX = end.X, PosY = end.Y, PosZ = end.Z };
    }
}

