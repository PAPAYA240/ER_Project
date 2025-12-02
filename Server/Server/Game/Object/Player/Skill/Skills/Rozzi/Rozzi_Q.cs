using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static Server.Data.DataUtils;

public sealed class Rozzi_Q : RozziSkillHandler
{
    private bool _isCollision = false;

    private float _elapsed = 0.0f;
    private float _StopSkillTime = 0.45f; 

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
        p.SendSkillEffect(ctx.MousePos, keyCode: _keyCode, sendLookatMousePacket: false);
    }

    public override void OnCollision<T>(Player p, List<T> targets, GameObject.StatusEffect effect)
    {
        _isCollision = true;

        foreach(var t in targets)
        {
            GameObject go = t as GameObject;
            if (go == null)
                return;

            p.SendSkillEffect(new Vector2(go.Position.X, go.Position.Z), keyCode: _keyCode, sendLookatMousePacket: false, 
                targetPos: default, targetRot: default, 
                type: "Select", "FX_BI_Rozzi_Skill02_Hit", 
                useTargetTransform: true, targetId: go.Id);
        }        
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
            p.SendRemoveEffect(_keyCode);
        }
    }

    public override void OnExit(Player p, SkillContext ctx)
    {
        if(_isCollision)
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
        }

        AddAttackToken(p);
    }
}

