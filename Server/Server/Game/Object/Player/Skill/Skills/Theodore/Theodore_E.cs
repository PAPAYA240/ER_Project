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

        _target = ObjectManager.Instance.Find(ctx.TargetId);

        _elapsed = 0.0f;

        _startPos = p.Position;
        _midPos = _target.Position;

        p.SendSkillConfirmPacket(true, ctx.Key, VariantKey.NoCollision);
    }

    public override void OnHit(Player p, SkillContext ctx)
    {
        return;
    }

    public override void OnTick(Player p, SkillContext ctx)
    {
        //float t = _elapsed / _animDuration;

        //Vector3 targetPos;

        //if (t < _followRatio)
        //{
        //    _midPos = _target.Position;

        //    float midT = t / _followRatio;
        //    targetPos = Vector3.Lerp(_startPos, _midPos, midT);
        //}
        //else
        //{
        //    float endT = (t - (1 - _followRatio)) / _followRatio;
        //    targetPos = Vector3.Lerp(_midPos, _midPos, endT);
        //}

        //_elapsed += TimeUtil.DeltaTime;

        //p.SendSkillMotion(
        // type: SkillMotionType.Transform,
        // start: p.Position,
        // end: targetPos);

        //Console.WriteLine($"targetPos : {targetPos}");
        
        return;
    }

    public override void OnExit(Player p, SkillContext ctx)
    {
        base.OnExit(p, ctx);
    }
}

