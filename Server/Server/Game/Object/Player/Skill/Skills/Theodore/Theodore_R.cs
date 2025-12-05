using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game;
using System;
using System.Numerics;
using static Server.Data.DataUtils;

public sealed class Theodore_R : SkillHandlerBase
{
    public override bool CanMoveDuringCast => false;
    public Theodore_R()
    {
        _characterType = CharacterType.Theodore;
        _animName = "SKILL_R";
        _keyCode = KeyCode.R;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);

        SendSkillConfirmPacket(p);
        p.LookAtMouse(ctx.MousePos);

        p.SendSkillEffect(ctx.MousePos, keyCode: _keyCode);
        p.SendSkillEffect(new Vector2(ctx.MousePos.X, ctx.MousePos.Y), keyCode: _keyCode, sendLookatMousePacket: false,
             targetPos: default, targetRot: default, type: "Select", "FX_Skill04_Charging");
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

