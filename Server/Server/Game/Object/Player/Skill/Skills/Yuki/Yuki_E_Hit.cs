using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static Server.Data.DataUtils;

public sealed class Yuki_E_Hit : SkillHandlerBase
{
    private Vector3 _startPos, _endPos, nextPos, _dir;
    private float _elapsed;
    private float _duration;
    private float _dashRange;       // 대쉬 이동거리
    private float _speed;

    public Yuki_E_Hit(Vector3 dir)
    {
        _characterType = CharacterType.Yuki;
        _animName = "SKILL_E_HIT";

        _dir = dir;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);
        // TODO: 코스트/쿨타임 차감

        _startPos = p.Position;

        // 초기화
        _dashRange = 1.5f;
        _elapsed = 0f;
        _speed = 17f;

        _endPos = _startPos + _dir * _dashRange;

        float distance = Vector3.Distance(_startPos, _endPos);

        _duration = distance / _speed;

        SendSkillCollisionRequestPacket(p, CollisionType.Block, _startPos, _endPos);
    }

    public override void OnHit(Player p, SkillContext ctx)
    {
        return;
    }

    public override void OnTick(Player p, SkillContext ctx)
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
            _elapsed += TimeUtil.DeltaTime;

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

        return;
    }

    public override void OnExit(Player p, SkillContext ctx)
    {
        base.OnExit(p, ctx);
    }
}
