using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static Server.Data.DataUtils;

public sealed class Yuki_R : SkillHandlerBase
{
    public Yuki_R()
    {
        _characterType = CharacterType.Yuki;
        _animName = "SKILL_R";
        _keyCode = KeyCode.R;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);
        // TODO: 코스트/쿨타임 차감

        SendSkillConfirmPacket(p);
        p.LookAtMouse(ctx.MousePos);
        p.SendYukiSkillEffect(ctx.MousePos);
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
        Console.WriteLine($"유키 단추 두번들어");
        base.OnExit(p, ctx);
    }
}
