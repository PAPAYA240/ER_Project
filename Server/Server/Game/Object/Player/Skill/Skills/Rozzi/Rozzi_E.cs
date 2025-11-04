using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using Server.Data;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static Server.Data.DataUtils;

public sealed class Rozzi_E : SkillHandlerBase
{
    private readonly float _followRatio = 0.4f;
    private readonly float _animDuration;

    private GameObject _target;

    private float _elapsed;
    private Vector3 _startPos, _midPos, _endPos;

    public Rozzi_E()
    {
        _characterType = CharacterType.Rozzi;
        _animName = "SKILL_E";
        _keyCode = KeyCode.E;

        _animDuration = GetDuration();
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);

        _target = ObjectManager.Instance.Find(ctx.TargetId);

        _elapsed = 0.0f;

        _startPos = p.Position;
        _midPos = _target.Position;

        p.SendSkillConfirmPacket(true, ctx.Key, VariantKey.Cast);
    }

    public override void OnHit(Player p, SkillContext ctx)
    {
        return;
    }

    public override void OnTick(Player p, SkillContext ctx)
    {
        float t = _elapsed / _animDuration;

        Vector3 targetPos = p.Position;

        if (t <= _followRatio)
        {
            _midPos = _target.Position;

            float midT = Math.Clamp(t / _followRatio, 0f, 1f);
            targetPos = Vector3.Lerp(_startPos, _midPos, midT);

            p.SendSkillMotion(
                type: SkillMotionType.Transform,
                start: p.Position,
                end: targetPos);
        }
        else
        {
            if (!_committed)
            {
                if (TryConsumeLatest(out SkillCollisionProposal prop))
                {
                    _committed = true;
                    _endPos = prop.BehindBlocked;
                }
            }
            else
            {
                float endT = (t - _followRatio) / (1f - _followRatio);
                endT = Math.Clamp(endT, 0f, 1f);
                targetPos = Vector3.Lerp(_midPos, _endPos, endT);

                p.SendSkillMotion(
                    type: SkillMotionType.Transform,
                    start: p.Position,
                    end: targetPos);
            }                
        }

        _elapsed += TimeUtil.DeltaTime;
      
        _finalEnd = targetPos;

        //Console.WriteLine($"targetPos : {targetPos}");

        return;
    }

    public override void OnExit(Player p, SkillContext ctx)
    {
        base.OnExit(p, ctx);

        p.SendSkillMotion(
         type: SkillMotionType.Transform,
         start: p.Position,
         end: _finalEnd,
         authoritativeEnd: true);
    }

    public override bool CanCast(Player p, SkillContext ctx)
    {
        _target = ObjectManager.Instance.Find(ctx.TargetId);
        SkillSpec spec = GetSkillSpec(true);
        if (_target == null || (_target != null && Vector3.Distance(_target.Position, p.Position) > spec.limits.baseMaxDist))
        {
            return false;
        }

        return true;
    }
}

