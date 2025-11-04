using System;
using System.Collections.Generic;
using System.Text;
using Google.Protobuf.Protocol;
using Server.Game;
using static Server.Data.DataUtils;


public sealed class Abigail_Q : Skill_Abigail
{
    public override bool CanMoveDuringCast => true;
    public override float MoveSpeedMultiplier => 1.2f;

    public Abigail_Q()
    {
        _animName = "SKILL_Q";
        _keyCode = KeyCode.Q;
        _animDuration = GetDuration();
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);
        // TODO: 코스트/쿨타임 차감

        p.SendSkillConfirmPacket(true, ctx.Key, VariantKey.NoCollision);
    }
}
