using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Text;
using static Server.Data.DataUtils;


public sealed class Hyunwoo_R_Loop : ChargingSkillHandler
{
    double _start;

    public Hyunwoo_R_Loop()
    {
        _characterType = CharacterType.Hyunwoo;
        _animName = "SKILL_R_LOOP";
        _keyCode = KeyCode.R;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);

        p.LookAtMouse(ctx.MousePos);
        _start = TimeUtil.UtcSec();
    }

    public override void OnHit(Player p, SkillContext ctx)
    {

    }
    public override void OnCharge(Player p, SkillContext ctx)
    {
        // when charging end
        p.ChargingRatio = 1;
        p.ChangeState(new Player_SkillState(SkillRegistry.Create("Hyunwoo_R_End"), ctx));
    }

    public override void OnTick(Player p, SkillContext ctx)
    {
        if (_start + 1.2 < TimeUtil.UtcSec())
        {
            p.ChargingRatio = 1;
            p.ChangeState(new Player_SkillState(SkillRegistry.Create("Hyunwoo_R_End"), ctx));
        }

    }

    public override void OnExit(Player p, SkillContext ctx)
    {
        base.OnExit(p, ctx);

    }
}

