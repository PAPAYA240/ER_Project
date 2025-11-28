using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using static Server.Data.DataUtils;

public sealed class Rozzi_Q_Dash : SkillHandlerBase
{
    private float _elapsed, _duration;
    private Vector3 _startPos, _endPos;

    private float _dashDistance = 3.0f;
    private float _dashSpeed = 20.0f;

    public Rozzi_Q_Dash()
    {
        _characterType = CharacterType.Rozzi;
        _animName = "SKILL_Q_DASH";
        _keyCode = KeyCode.Q;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);

        Vector3 mousePos = new Vector3(ctx.MousePos.X, p.Position.Y, ctx.MousePos.Y);
        Vector3 dir = Vector3.Normalize(mousePos - p.Position);
        Vector3 targetPos = p.Position + dir * _dashDistance;

        SendSkillCollisionRequestPacket(p, CollisionType.Block, p.Position, targetPos);
        p.LookAtMouse(ctx.MousePos);

        p.SendSkillEffect(ctx.MousePos, keyCode: _keyCode, sendLookatMousePacket: true, targetPos: default, targetRot: default, type: "Select", "FX_BI_Rozzi_Skill01_Move");
    }

    public override void OnHit(Player p, SkillContext ctx)
    {
        return;
    }

    public override void OnTick(Player p, SkillContext ctx)
    {
        if (_requestId != _commitId)
        { 
            if(TryConsumeLatest(ref _commitId, out SkillCollisionProposal prop))
            {
                _startPos = p.Position;
                _endPos = prop.collisionPos;

                _duration = Vector3.Distance(_startPos, _endPos) / _dashSpeed;
            }                
        }

        if (_requestId == _commitId)
        {
            float t = Math.Clamp(_elapsed / _duration, 0f, 1f);
            Vector3 targetPos = Vector3.Lerp(_startPos, _endPos, t);

            p.SendSkillMotion(
             type: SkillMotionType.Transform,
             start: p.Position,
             end: targetPos);

            _finalEnd = targetPos;

            _elapsed += TimeUtil.Instance.DeltaTime;
            if (_elapsed > _duration)
            {
                ctx.RequestFinish();
            }
        }         
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
}

