using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static Server.Data.DataUtils;

public sealed class Skill_Blink : SkillHandlerBase
{
    public Skill_Blink()
    {
        _keyCode = KeyCode.F;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        LastSeq = 0;
        Latest = default;
        _committed = false;

        p.SendStopPacket(StopReason.StopMoveOnly);
        //p.SendSkillConfirmPacket(true, ctx.Key, VariantKey.Cast);

        Vector3 targetPos = new Vector3(ctx.MousePos.X, p.Position.Y, ctx.MousePos.Y);
        p.SendSkillCollisionRequestPacket(_keyCode, CollisionType.Pass, p.Position, targetPos);
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

        p.SendSkillMotion(
         type: SkillMotionType.Transform,
         start: p.Position,
         end: prop.collisionPos);

        _committed = true;
    }

    public override void OnExit(Player p, SkillContext ctx)
    {
        base.OnExit(p, ctx);
    }
}

