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
    private Vector3 _startPos, _endPos, nextPos, _collisionPos, _dir;
    private float _elapsed;
    private float _duration = 0.3f;
    private float _dashRange;

    private GameObject enemy;

    public Yuki_E()
    {
        _characterType = CharacterType.Yuki;
        _animName = "SKILL_E";
        _keyCode = KeyCode.W;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);
        // TODO: 코스트/쿨타임 차감

        _startPos = p.Position;

        _dashRange = 5.0f;

        Vector3 mouseWorldPos = new Vector3(ctx.MousePos.X, p.Position.Y, ctx.MousePos.Y);

        _dir = mouseWorldPos - p.Position;
        _dir.Y = 0;
        _dir = Vector3.Normalize(_dir);

        _endPos = _startPos + _dir * _dashRange;

        _elapsed = 0f;

        //p.SendSkillConfirmPacket(true, ctx.Key, VariantKey.Cast);
        Vector3 targetPos = _endPos;
        p.SendSkillCollisionRequestPacket(_keyCode, CollisionType.Clamp, p.Position, targetPos);

        p.LookAtMouse(ctx.MousePos);
    }

    public override void OnHit(Player p, SkillContext ctx)
    {
        return;
    }

    public override void OnCollision(Player p, GameObject obj)
    {
        enemy = obj;

        _startPos = p.Position;

        // 충돌 시점 저장
        _collisionPos = obj.Position;

        _dashRange = 1.0f;
        _endPos = _collisionPos + _dir * _dashRange;

        _elapsed = 0f;
        _duration = _dashRange / 10f;

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
                nextPos = Vector3.Lerp(_startPos, _endPos, _elapsed / _duration);

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
