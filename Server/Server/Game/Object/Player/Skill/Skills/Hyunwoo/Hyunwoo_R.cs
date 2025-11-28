using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static Server.Data.DataUtils;


public sealed class Hyunwoo_R : ChargingSkillHandler
{
    double _start;

    public Hyunwoo_R()
    {
        _characterType = CharacterType.Hyunwoo;
        _animName = "SKILL_R";
        _keyCode = KeyCode.R;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);

        SendSkillConfirmPacket(p);
        p.LookAtMouse(ctx.MousePos);
        p.SendSkillEffect(ctx.MousePos, keyCode: _keyCode);
        _start = TimeUtil.UtcSec();
    }

    public override void OnHit(Player p, SkillContext ctx)
    {
        
    }

    public override void OnCharge(Player p, SkillContext ctx)
    {
        // when charging end

        ISkill skillhandler;

        if (p.ChargingRatio < 1) // short charge
            skillhandler = SkillRegistry.Create("Hyunwoo_R_Short_End");
        else // full charge
            skillhandler = SkillRegistry.Create("Hyunwoo_R_End");

        p.ChangeState(new Player_SkillState(skillhandler, ctx));
    }

    public override void OnTick(Player p, SkillContext ctx)
    {
        if(_start + 2 < TimeUtil.UtcSec())
            p.ChangeState(new Player_SkillState(SkillRegistry.Create("Hyunwoo_R_Loop"), ctx));
    }

    public override void OnExit(Player p, SkillContext ctx)
    {
        base.OnExit(p, ctx);

        _start = 0;
    }
}

