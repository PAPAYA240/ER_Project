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

        p.SendSkillConfirmPacket(true, ctx.Key, VariantKey.Followup);
    }

    public override void OnHit(Player p, SkillContext ctx)
    {
        return;
    }

    public override void OnTick(Player p, SkillContext ctx)
    {
        if (TryConsumeLatest(out var prop))
        {
            _startPos = p.Position;
            _endPos = prop.EndBlocked;

            _duration = Vector3.Distance(_startPos, _endPos) / _spec.limits.speed;

            _committed = true;
        }

        if (!_committed)
            return;

        float t = Math.Clamp(_elapsed / _duration, 0f, 1f);
        Vector3 targetPos = Vector3.Lerp(_startPos, _endPos, t);
        Console.WriteLine($"t : {t}, targetPos : {targetPos}");

        p.SendSkillMotion(
         type: SkillMotionType.Transform,
         start: p.Position,
         end: targetPos);

        _elapsed += TimeUtil.DeltaTime;
        if(_elapsed > _duration)
        {
            if(p.CurrentState is Player_SkillState skill)
            {
                skill.RequestFinish();
                return;
            }    
        }
    }

    public override void OnExit(Player p, SkillContext ctx)
    {
        base.OnExit(p, ctx);
    }

    private void CommitMotionOnce(Player p, Vector3 from, Vector3 end)
    {
        _finalEnd = end;

        float dist = Vector3.Distance(from, end);
        //float speed = /*spec.limits.speed*/ /*dist / GetDuration()*/ 4.0f;
        float duration = MathF.Max(0.05f, GetDuration() /*dist / speed*/);

        p.SendSkillMotion(
             type: SkillMotionType.Transform,
             start: from,
             end: _finalEnd,
             duration: duration,
             anim: ""/*spec.AnimName*/,
             curveId: "EaseOutCubic",
             serverCollision: true,
             authoritativeEnd: true);

        p.Flags.IsInSkillMotion = true;
    }
}

