using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

public sealed class Rozzi_Q : SkillHandlerBase
{
    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);

        p.SendAnimPacket("SKILL_Q", 0.05f);         // 애니 브로드캐스트
                                                    // TODO: 코스트/쿨타임 차감

        p.SendSkillConfirmPacket(ctx.Key, VariantKey.Followup);
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
        var end = prop.EndBlocked;

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
        float speed = /*spec.limits.speed*/4.0f;
        float duration = MathF.Max(0.05f, dist / speed);

        p.SendSkillMotion(
             type: SkillMotionType.Dash,
             start: from,
             end: _finalEnd,
             duration: duration,
             anim: ""/*spec.AnimName*/,
             curveId: "EaseOutCubic",
             serverCollision: true,
             authoritativeEnd: true);

        p.Flags.IsInSkillMotion = true;
    }
}

