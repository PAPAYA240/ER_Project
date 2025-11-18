using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static Server.Data.DataUtils;

public sealed class Rozzi_D : SkillHandlerBase
{
    public override bool CanMoveDuringCast => true;
    public override float MoveSpeedMultiplier => 1.2f;

    public Rozzi_D()
    {
        _characterType = CharacterType.Rozzi;
        _animName = "SKILL_D";
        _keyCode = KeyCode.D;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);

        p.Room.AddStatusEffect(p, p, _keyCode, null);

        // 로지 공속버프용입니다 연진님
        p.AttackSpeedBuff(0.7f, 2);

        SendSkillConfirmPacket(p);
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

