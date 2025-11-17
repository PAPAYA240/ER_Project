using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using Server.Game;
using static Server.Data.DataUtils;

public sealed class Abigail_D : Skill_Abigail
{
    private Vector3 _startPos, _endPos, nextPos, _dir;
    private float _dashRange;
    private float _duration = 0.1f;

    public Abigail_D()
    {
        _animName = "SKILL_D";
        _keyCode = KeyCode.D;
        _animDuration = GetDuration();
        StopSkillTime = 0.1f; // 3프레임 * 30FPS
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);

        _startPos = p.Position;

        // 초기화
        _dashRange = 2f;
        _elapsed = 0f;

        Vector3 mouseWorldPos = new Vector3(ctx.MousePos.X, p.Position.Y, ctx.MousePos.Y);

        _dir = mouseWorldPos - p.Position;
        _dir.Y = 0;
        _dir = Vector3.Normalize(_dir);

        _endPos = _startPos + _dir * _dashRange;

        Vector3 targetPos = _endPos;
        SendSkillCollisionRequestPacket(p, CollisionType.Block, _startPos, targetPos);

        SendSkillConfirmPacket(p);
        p.LookAtMouse(new Vector2(ctx.MousePos.X, ctx.MousePos.Y));
    }

    public override void OnTick(Player p, SkillContext ctx)
    {
        if (CanStopSkill)
            return;

        _elapsed += TimeUtil.Instance.DeltaTime;

        if (_elapsed >= StopSkillTime)
        {
            CanStopSkill = true;
            p.SendCanStopSkillPacket(CanStopSkill);
        }
        else
        {
            if (_requestId != _commitId)
            {
                if (TryConsumeLatest(ref _commitId, out SkillCollisionProposal prop))
                {
                    _startPos = p.Position;
                    _endPos = prop.collisionPos;
                }
            }

            if (_requestId == _commitId)
            {
                _elapsed += TimeUtil.Instance.DeltaTime;

                if (_elapsed < _duration)
                {
                    float t = Math.Clamp(_elapsed / _duration, 0f, 1f);
                    nextPos = Vector3.Lerp(_startPos, _endPos, t);
                }

                p.SendSkillMotion(
                    type: SkillMotionType.Transform,
                    start: p.Position,
                    end: nextPos
                );
            }
        }
    }
}
