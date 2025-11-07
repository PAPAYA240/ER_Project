using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static Server.Data.DataUtils;

public sealed class Rozzi_Q : SkillHandlerBase
{
    private float _coolDownReductionValue = 2.0f;

    public Rozzi_Q()
    {
        _characterType = CharacterType.Rozzi;
        _animName = "SKILL_Q";
        _keyCode = KeyCode.Q;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);

        SendSkillConfirmPacket(p);
        p.LookAtMouse(ctx.MousePos);
    }

    public override void OnCollision(Player p)
    {
        p.Tokens.Add(new NextInputToken
        {
            Active = true,
            RemainingUses = 1,
            ExpireUtc = TimeUtil.UtcSec() + 2.0,
            Priority = 10,
            Trigger = InputKind.Move,
            ReplacementSkillKey = "Rozzi_Q_Dash",
            CancelOnUseSkill = true
        });

        p.Skill.Reduce(_keyCode, _coolDownReductionValue);
    }
}

