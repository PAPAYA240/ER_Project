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
        //p.LookAtMouse(ctx.MousePos);
    }
}
