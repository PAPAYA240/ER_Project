using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using Server.Data;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static Server.Data.DataUtils;

public sealed class Theodore_E : SkillHandlerBase
{
    private readonly float _followRatio = 0.4f;
    private readonly float _animDuration;
    private readonly float _behindOffset = 1.0f;

    private GameObject _target;

    private float _elapsed;
    private Vector3 _startPos, _midPos;

    private bool _canUse = true;

    public Theodore_E()
    {
        _characterType = CharacterType.Theodore;
        _animName = "SKILL_E";
        _keyCode = KeyCode.E;
        
        _animDuration = GetDuration();
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);

        Projectile projectile = ObjectManager.Instance.Add<Projectile>();
        if (projectile != null)
        {
            projectile.Owner = p;
            projectile.Init();
            p.Room.EnterGame(projectile);
        }
    }

    public override void OnHit(Player p, SkillContext ctx)
    {
        return;
    }

    public override void OnTick(Player p, SkillContext ctx)
    {
    }

    public override void OnExit(Player p, SkillContext ctx)
    {
        base.OnExit(p, ctx);
    }
}

