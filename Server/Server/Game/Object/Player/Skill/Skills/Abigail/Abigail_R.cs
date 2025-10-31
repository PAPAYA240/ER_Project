using System;
using System.Collections.Generic;
using System.Text;
using Google.Protobuf.Protocol;
using Server.Game;
using static Server.Data.DataUtils;


public sealed class Abigail_R : SkillHandlerBase
{
    public Abigail_R()
    {
        _characterType = CharacterType.Abigail;
        _animName = "SKILL_R";
        _keyCode = KeyCode.R;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);
        // TODO: 코스트/쿨타임 차감

        p.SendSkillConfirmPacket(true, ctx.Key, VariantKey.NoCollision);
    }
}
