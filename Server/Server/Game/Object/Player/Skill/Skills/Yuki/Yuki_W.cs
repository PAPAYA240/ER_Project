using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Text;
using static Server.Data.DataUtils;

public sealed class Yuki_W : SkillHandlerBase
{
    public Yuki_W()
    {
        _characterType = CharacterType.Yuki;
        _animName = "SKILL_W";
        _keyCode = KeyCode.W;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);
        // TODO: 코스트/쿨타임 차감

        SendSkillConfirmPacket(p);

        p.Room.AddStatusEffect(p, p, _keyCode, null);

        Console.WriteLine("두번 들어옴?");
        //p.LookAtMouse(ctx.MousePos);
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
        p.YukiStud = 4;
        Console.WriteLine($"유키 단추 두번들어오나? : {p.YukiStud}");
        
        base.OnExit(p, ctx);
    }
}
