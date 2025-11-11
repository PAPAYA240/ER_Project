using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Text;
using static Server.Data.DataUtils;


public sealed class Hyunwoo_Q : SkillHandlerBase
{
    public Hyunwoo_Q()
    {
        _characterType = CharacterType.Hyunwoo;
        _animName = "SKILL_Q";
        _keyCode = KeyCode.Q;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);

        SendSkillConfirmPacket(p);
        p.LookAtMouse(ctx.MousePos);
    }

    public override void OnHit(Player p, SkillContext ctx)
    {

    }

    public override void OnTick(Player p, SkillContext ctx)
    {

    }

    public override void OnExit(Player p, SkillContext ctx)
    {
        base.OnExit(p, ctx);

    }
}

