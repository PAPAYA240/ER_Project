using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static Server.Data.DataUtils;

public sealed class Rozzi_W : SkillHandlerBase
{
    public override bool CanMoveDuringCast => true;
    public override float MoveSpeedMultiplier => 1.2f;

    public Rozzi_W()
    {
        _characterType = CharacterType.Rozzi;
        _animName = "SKILL_W";
        _keyCode = KeyCode.W;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);

        p.SendSkillConfirmPacket(true, ctx.Key, VariantKey.NoCollision);
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

    //private void CommitMotionOnce(Player p, Vector3 from, Vector3 end)
    //{
        //_finalEnd = end;

        //float dist = Vector3.Distance(from, end);
        //float speed = 4.0f;
        //float duration = MathF.Max(0.05f, dist / speed);

        //p.SendSkillMotion(
        //     type: SkillMotionType.Dash,
        //     start: from,
        //     end: _finalEnd,
        //     duration: duration,
        //     anim: ""/*spec.AnimName*/,
        //     curveId: "EaseOutCubic",
        //     serverCollision: true,
        //     authoritativeEnd: true);

        //p.Flags.IsInSkillMotion = true;
    //}
}

