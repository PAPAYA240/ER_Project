using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static Server.Data.DataUtils;

public sealed class Rozzi_R : RozziSkillHandler
{
    private float _elapsed = 0.0f;
    private float _StopSkillTime = 0.4f;

    public Rozzi_R()
    {
        _characterType = CharacterType.Rozzi;
        _animName = "SKILL_R";
        _keyCode = KeyCode.R;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);
        p.LookAtMouse(ctx.MousePos);
        SendSkillConfirmPacket(p);

        Projectile_Rozzi_R projectile = ObjectManager.Instance.Add<Projectile_Rozzi_R>();
        if (projectile != null)
        {
            projectile.ProjectileType = ProjectileType.ProjectileRozziR;
            projectile.Owner = p;
            projectile.Init();
            p.Room.Push(p.Room.EnterGame, projectile, 0);
            projectile.BOMBSTATE = BOMB_ROZZI.Flying;
        }

        p.SendSkillEffect(ctx.MousePos, keyCode: _keyCode, sendLookatMousePacket: false);
    }

    public override void OnTick(Player p, SkillContext ctx)
    {
        if (CanStopSkill)
            return;

        _elapsed += TimeUtil.Instance.DeltaTime;
        if (_elapsed >= _StopSkillTime)
        {
            CanStopSkill = true;
            p.SendCanStopSkillPacket(CanStopSkill);
        }
    }

    public override void OnExit(Player p, SkillContext ctx)
    {
        AddAttackToken(p);
    }
}

