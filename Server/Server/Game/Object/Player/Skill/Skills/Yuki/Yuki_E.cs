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
    private Vector3 _startPos, _endPos, nextPos;
    private float _elapsed;
    private float _duration = 0.3f;

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

        const float dashRange = 5.0f;

        Vector3 mouseWorldPos = new Vector3(ctx.MousePos.X, p.Position.Y, ctx.MousePos.Y);

        Vector3 dir = mouseWorldPos - p.Position;
        dir.Y = 0;
        dir = Vector3.Normalize(dir);

        _endPos = _startPos + dir * dashRange;

        _elapsed = 0f;

        //p.SendSkillConfirmPacket(true, ctx.Key, VariantKey.Cast);
        Vector3 targetPos = _endPos;
        p.SendSkillCollisionRequestPacket(_keyCode, CollisionType.Clamp, p.Position, targetPos);
        ////////////////////p.SendSkillCostPacket(_keyCode, p.GetCoolTime(_keyCode));
        p.LookAtMouse(ctx.MousePos);
    }

    public override void OnHit(Player p, SkillContext ctx)
    {
        return;
    }

    public override void OnCollision(Player p)
    {
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
