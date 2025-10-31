using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static Server.Data.DataUtils;

public sealed class Rozzi_Q_Dash : SkillHandlerBase
{
    private float _elapsed, _duration;
    private Vector3 _startPos, _endPos;

    SkillSpec _spec;

    public Rozzi_Q_Dash()
    {
        _characterType = CharacterType.Rozzi;
        _animName = "SKILL_Q_DASH";
        _keyCode = KeyCode.Q;

        _spec = GetSkillSpec(false);
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);

        _elapsed = 0.0f;
        _committed = false;

        p.SendSkillConfirmPacket(true, ctx.Key, VariantKey.Followup);
    }

    public override void OnHit(Player p, SkillContext ctx)
    {
        return;
    }

    public override void OnTick(Player p, SkillContext ctx)
    {       
        if (!_committed)
        {
            if (TryConsumeLatest(out var prop))
            {
                _startPos = p.Position;
                _endPos = prop.EndBlocked;

                _duration = Vector3.Distance(_startPos, _endPos) / _spec.limits.speed;

                _committed = true;
            }
        }
        else
        {
            float t = Math.Clamp(_elapsed / _duration, 0f, 1f);
            Vector3 targetPos = Vector3.Lerp(_startPos, _endPos, t);

            p.SendSkillMotion(
             type: SkillMotionType.Transform,
             start: p.Position,
             end: targetPos);

            _elapsed += TimeUtil.DeltaTime;
            if (_elapsed > _duration)
            {
                ctx.RequestFinish();
            }
        }         
    }

    public override void OnExit(Player p, SkillContext ctx)
    {
        base.OnExit(p, ctx);
    }
}

