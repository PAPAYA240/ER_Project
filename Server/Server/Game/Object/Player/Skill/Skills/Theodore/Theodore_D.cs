using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static Server.Data.DataUtils;

public sealed class Theodore_D : SkillHandlerBase
{
    public override bool CanMoveDuringCast => true;
    public override float MoveSpeedMultiplier => 1.2f;

    public Theodore_D()
    {
        _characterType = CharacterType.Theodore;
        _animName = "SKILL_D";
        _keyCode = KeyCode.D;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        //if ()
        //{
        //    //조준
        //    p.SendAnimPacket("CHARGING", 0.05f);
        //}
        //base.OnEnter(p, ctx);
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

