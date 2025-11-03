using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static Server.Data.DataUtils;

public sealed class Theodore_Q : SkillHandlerBase
{
    public Theodore_Q()
    {
        _characterType = CharacterType.Theodore;
        _animName = "SKILL_Q";
        _keyCode = KeyCode.Q;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);
    }

    public override void OnHit(Player p, SkillContext ctx)
    {
        return;
    }

    // TEMP
    bool isCommited = false;
    public override void OnTick(Player p, SkillContext ctx)
    {
        if (isCommited)
            return;

        isCommited = true;  

        return;
    }

    public override void OnExit(Player p, SkillContext ctx)
    {
        base.OnExit(p, ctx);
    }
}

