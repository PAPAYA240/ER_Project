using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Text;
using static Server.Data.DataUtils;

public sealed class Yuki_R : SkillHandlerBase
{
    public Yuki_R()
    {
        _characterType = CharacterType.Yuki;
        _animName = "SKILL_R";
        _keyCode = KeyCode.W;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);
        // TODO: 코스트/쿨타임 차감

        SendSkillConfirmPacket(p);
        p.LookAtMouse(ctx.MousePos);
    }
}
