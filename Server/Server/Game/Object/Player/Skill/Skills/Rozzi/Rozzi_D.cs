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

    private float _elapsed;
    private float _duration = 1.0f;

    public Rozzi_D()
    {
        _characterType = CharacterType.Rozzi;
        _animName = "SKILL_D";
        _keyCode = KeyCode.D;

        HitboxCreated = false;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        //base.OnEnter(p, ctx);

        // 전투 모드
        {
            p.CombatState = CombatState.Combat;
            S_CombatMode combatModePkt = new S_CombatMode();
            combatModePkt.ObjectId = p.Id;
            combatModePkt.CombatMode = p.CombatState;
            p.Room.Broadcast(combatModePkt);
            p.CombatTime = 0f;
        }

        // 애니메이션 패킷 전송
        p.SendAnimPacket("ROZZI_D", 0.05f);

        if (HitboxCreated)
            CreateHitbox(p, ctx);

        p.Room.AddStatusEffect(p, p, _keyCode, null);   // 이속 증가

        SendSkillConfirmPacket(p);
    }

    public override void OnHit(Player p, SkillContext ctx)
    {
        return;
    }

    public override void OnTick(Player p, SkillContext ctx)
    {
        _elapsed += TimeUtil.Instance.DeltaTime;
        if (_elapsed > _duration)
            ctx.RequestFinish();

        return;
    }

    public override void OnExit(Player p, SkillContext ctx)
    {
        base.OnExit(p, ctx);

        p.AttackSpeedBuff(0.7f, 2);
    }
}

