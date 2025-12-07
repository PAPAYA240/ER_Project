using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Data;
using System.Numerics;
using System.Text;
using static Player_StunState;
using static Server.Data.DataUtils;


public sealed class Hyunwoo_E : SkillHandlerBase
{
    private Vector3 _startPos, _endPos, nextPos, _dir;
    private float _elapsed;
    private float _duration;
    private float _dashRange;       // 대쉬 이동거리
    private float _knockbackRange;
    private float _speed;

    Dictionary<int, KeyValuePair<Player, Vector3>> _players = new Dictionary<int, KeyValuePair<Player, Vector3>>();

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

        // Init values
        _dashRange = 5.0f;
        _knockbackRange = 3.0f;
        _elapsed = 0f;
        _speed = 17f;

        Vector3 mouseWorldPos = new Vector3(ctx.MousePos.X, p.Position.Y, ctx.MousePos.Y);

        _dir = mouseWorldPos - p.Position;
        _dir.Y = 0;
        _dir = Vector3.Normalize(_dir);

        // calculate End Position
        _endPos = _startPos + _dir * _dashRange;

        _duration = _dashRange / _speed;

        // Request collision position to client
        SendSkillCollisionRequestPacket(p, CollisionType.Block, p.Position, _endPos);
        p.SendSkillCostPacket(_keyCode);

        p.LookAtMouse(ctx.MousePos);
    }

    public override void OnHit(Player p, SkillContext ctx)
    {

    }

    public override void OnCollision<T>(Player p, List<T> targets, GameObject.StatusEffect effect)
    {
        foreach (var targetObject in targets)
        {
            if(targetObject is Player targetPlayer)
            {
                if (targetPlayer.IsUnstoppable())
                    continue;

                Vector3 targetPos = targetPlayer.Position;
                Vector3 endPos = targetPos + _dir * _knockbackRange;

                SendSkillCollisionRequestPacket(p, CollisionType.Block, targetPos, endPos);
                _players.Add(_requestId, new KeyValuePair<Player, Vector3>(targetPlayer, targetPos));
            }
        }

        if (p is Hyunwoo hyunwoo)
        {
            hyunwoo.AddTSkillCount(1);
        }
    }

    public override void OnTick(Player p, SkillContext ctx)
    {
        // process a request
        if (_requestId != _commitId)
        {
            if (TryConsumeLatest(ref _commitId, out SkillCollisionProposal prop))
            {
                if (_players.TryGetValue(_commitId, out KeyValuePair<Player, Vector3> tartgetKVP))
                {
                    // Knockback
                    Vector3 start = tartgetKVP.Value;
                    Vector3 end = prop.collisionPos;

                    StunStateDesc desc = new StunStateDesc();
                    desc.EndPos = end;
                    desc.Speed = _speed * 2f;

                    // hit the wall
                    if ((start - end).Length() - _knockbackRange < float.Epsilon)
                        desc.Duration = _duration + 1.2f;
                    // do not hit the wall
                    else
                        desc.Duration = _duration ;

                    if(!tartgetKVP.Key.IsDead)
                        tartgetKVP.Key.ChangeState(new Player_StunState(desc));
                }
                else
                {
                    // Hyunwoo Moving
                    _startPos = p.Position;
                    _endPos = prop.collisionPos;

                    _duration = (_startPos - _endPos).Length() / _speed;
                }
            }
        }

        if (_requestId == _commitId)
        {
            _elapsed += TimeUtil.Instance.DeltaTime;

            if (_elapsed < _duration)
            {
                // calc move position
                float t = Math.Clamp(_elapsed / _duration, 0f, 1f);
                nextPos = Vector3.Lerp(_startPos, _endPos, t);
            }
            else
            {
                // hit the wall
                if(_dashRange - (_startPos - _endPos).Length() > 0.1f)
                {
                    p.ChangeState(new Player_SkillState(SkillRegistry.Create("Hyunwoo_E_End"), ctx));
                }
                // do not hit the wall
                else
                {
                    p.ChangeState(new Player_IdleState());
                }
                return;
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

