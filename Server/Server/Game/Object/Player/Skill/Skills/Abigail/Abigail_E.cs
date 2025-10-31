using System;
using System.Collections.Generic;
using System.Text;
using Google.Protobuf.Protocol;
using Server.Game;
using static Server.Data.DataUtils;


public sealed class Abigail_E : SkillHandlerBase
{
    public Abigail_E()
    {
        _characterType = CharacterType.Abigail;
        _animName = "SKILL_E";
        _keyCode = KeyCode.E;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);
        // TODO: 코스트/쿨타임 차감

        p.SendSkillConfirmPacket(true, ctx.Key, VariantKey.NoCollision);
    }
}
