using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static Server.Data.DataUtils;


public sealed class Hyunwoo_E : SkillHandlerBase
{
    private Vector3 _startPos, _endPos, nextPos, _dir;
    private float _elapsed;
    private float _duration;
    private float _dashRange;       // 대쉬 이동거리
    private float _speed;

    public Hyunwoo_E()
    {
        _characterType = CharacterType.Hyunwoo;
        _animName = "SKILL_E";
        _keyCode = KeyCode.E;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);

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
        p.SendSkillCollisionRequestPacket(_keyCode, CollisionType.Block, p.Position, targetPos);
        p.SendSkillCostPacket(_keyCode);

        p.LookAtMouse(ctx.MousePos);

        //p.SendSkillConfirmPacket(true, ctx.Key, VariantKey.NoCollision);
        //p.LookAtMouse(ctx.MousePos);
    }

    public override void OnHit(Player p, SkillContext ctx)
    {

    }

    public override void OnTick(Player p, SkillContext ctx)
    {
        if (!_committed)
        {
            if (TryConsumeLatest(out SkillCollisionProposal prop))
            {
                _startPos = p.Position;
                _endPos = prop.EndBlocked;
                _committed = true;

                _duration = (_startPos - _endPos).Length() / _speed;
            }
        }

        if (_committed)
        {
            _elapsed += TimeUtil.DeltaTime;

            if (_elapsed < _duration)
            {
                float t = Math.Clamp(_elapsed / _duration, 0f, 1f);
                nextPos = Vector3.Lerp(_startPos, _endPos, t);
            }
            else
            {
                if(_dashRange - (_startPos - _endPos).Length() > 0.1f)
                {
                    p.ChangeState(new Player_SkillState(SkillRegistry.Create("Hyunwoo_E_End"), ctx));
                }
                else
                {
                    p.ChangeState(new Player_IdleState());
                }

            }

            p.SendSkillMotion(
                type: SkillMotionType.Transform,
                start: p.Position,
                end: nextPos
            );
        }


    }

    public override void OnExit(Player p, SkillContext ctx)
    {
        base.OnExit(p, ctx);
    }
}

