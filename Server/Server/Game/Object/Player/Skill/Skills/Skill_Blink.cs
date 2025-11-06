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
    private float _blinkDistance = 3.0f;

    public Skill_Blink()
    {
        _keyCode = KeyCode.F;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        //LastSeq = 0;
        //Latest = default;
        //_committed = false;

        p.SendStopPacket(StopReason.StopMoveOnly);
        p.SendSkillCostPacket(_keyCode);

        Vector3 mousePos = new Vector3(ctx.MousePos.X, p.Position.Y, ctx.MousePos.Y);
        Vector3 dir = Vector3.Normalize(mousePos - p.Position);
        Vector3 targetPos = p.Position + dir * _blinkDistance;

        SendSkillCollisionRequestPacket(p, CollisionType.Pass, p.Position, targetPos);
    }

    public override void OnHit(Player p, SkillContext ctx)
    {
        return;
    }

    public override void OnTick(Player p, SkillContext ctx)
    {
        if (_requestId != _commitId)
        {
            if (TryConsumeLatest(ref _commitId, out SkillCollisionProposal prop))
            {
                p.SendSkillMotion(
                    type: SkillMotionType.Transform,
                    start: p.Position,
                    end: prop.collisionPos);
            }
        }
    }

    public override void OnExit(Player p, SkillContext ctx)
    {
        base.OnExit(p, ctx);
    }
}

