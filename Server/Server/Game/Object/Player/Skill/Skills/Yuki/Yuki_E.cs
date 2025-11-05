using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static Server.Data.DataUtils;

public sealed class Yuki_E : SkillHandlerBase
{
    private Vector3 _startPos, _endPos, nextPos, _dir;
    private float _elapsed;
    private float _duration;
    private float _dashRange;       // 대쉬 이동거리
    private float _speed;

    public Yuki_E()
    {
        _characterType = CharacterType.Yuki;
        _animName = "SKILL_E";
        _keyCode = KeyCode.E;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);
        // TODO: 코스트/쿨타임 차감

        _startPos = p.Position;

        // 초기화
        _dashRange = 5.0f;
        _elapsed = 0f;
        _speed = 17f;

        Vector3 mouseWorldPos = new Vector3(ctx.MousePos.X, p.Position.Y, ctx.MousePos.Y);

        _dir = mouseWorldPos - p.Position;
        _dir.Y = 0;
        _dir = Vector3.Normalize(_dir);

        _endPos = _startPos + _dir * _dashRange;

        _duration = _dashRange / _speed;

        Vector3 targetPos = _endPos;
        p.SendSkillCollisionRequestPacket(_keyCode, CollisionType.Clamp, p.Position, targetPos);
        p.SendSkillCostPacket(_keyCode);

        p.LookAtMouse(ctx.MousePos);
    }

    public override void OnHit(Player p, SkillContext ctx)
    {
        return;
    }

    public override void OnCollision(Player p)
    {
        _committed = false;

        _startPos = p.Position;

        _dashRange = 1.5f;
        _endPos = _startPos + _dir * _dashRange;

        _elapsed = 0f;

        float distance = Vector3.Distance(_startPos, _endPos);

        _duration = distance / _speed;

        p.SendSkillCollisionRequestPacket(_keyCode, CollisionType.Block, _startPos, _endPos);

        return;
    }

    public override void OnTick(Player p, SkillContext ctx)
    {
        if (!_committed)
        {
            if (TryConsumeLatest(out SkillCollisionProposal prop))
            {
                _startPos = p.Position;
                _endPos = prop.BehindBlocked;
                _committed = true;
            }
        }

        if(_committed)
        {
            if (_elapsed < _duration)
            {
                float t = Math.Clamp(_elapsed / _duration, 0f, 1f);
                nextPos = Vector3.Lerp(_startPos, _endPos, _elapsed / _duration);
            }

            _elapsed += TimeUtil.DeltaTime;
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
