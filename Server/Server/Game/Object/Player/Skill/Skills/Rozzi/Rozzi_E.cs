using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using Server.Data;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static Player_StunState;
using static Server.Data.DataUtils;

public sealed class Rozzi_E : SkillHandlerBase
{
    private readonly float _followRatio = 0.4f;
    private readonly float _animDuration;

    private GameObject _target;

    private float _elapsed, _duration;
    private Vector3 _startPos, _midPos, _endPos, _dir;

    private float _dashDistance = 3f;

    private float _behindDistance = 2.5f;
    private float _behindSpeed = 10.0f;

    private float _stunDuration = 0.5f;

    private bool _isRequest;

    public Rozzi_E()
    {
        _characterType = CharacterType.Rozzi;
        _animName = "SKILL_E";
        _keyCode = KeyCode.E;

        _duration = _animDuration = GetDuration();

        HitboxRequired = false;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);

        _target = ObjectManager.Instance.Find(ctx.TargetId);
        if(_target == null)
        {

        }

        _startPos = p.Position;
        _midPos = _target.Position;
        _dir = Vector3.Normalize(_midPos - _startPos);

        SendSkillConfirmPacket(p);
    }

    public override void OnHit(Player p, SkillContext ctx)
    {
        return;
    }

    public override void OnTick(Player p, SkillContext ctx)
    {
        float t = _elapsed / _animDuration;

        Vector3 targetPos = p.Position;

        if (t <= _followRatio)
        {
            _midPos = _target.Position;

            float midT = Math.Clamp(t / _followRatio, 0f, 1f);
            targetPos = Vector3.Lerp(_startPos, _midPos, midT);

            p.SendSkillMotion(
                type: SkillMotionType.Transform,
                start: p.Position,
                end: targetPos);           
        }
        else
        {
            if (_requestId != _commitId)
            {
                if (TryConsumeLatest(ref _commitId, out SkillCollisionProposal prop))
                {
                    _startPos = p.Position;
                    _endPos = prop.collisionPos;
                    _duration = Vector3.Distance(_startPos, _endPos) / _behindSpeed + _elapsed;
                }
            }
            
            if(_requestId == _commitId) 
            {
                float endT = (t - _followRatio) / (1f - _followRatio);
                endT = Math.Clamp(endT, 0f, 1f);
                targetPos = Vector3.Lerp(_midPos, _endPos, endT);

                p.SendSkillMotion(
                    type: SkillMotionType.Transform,
                    start: p.Position,
                    end: targetPos);
            }                
        }

        if(!_isRequest && t >= _followRatio)
        {          
            Vector3 requestPos = p.Position + _dir * _behindDistance;
            SendSkillCollisionRequestPacket(p, CollisionType.Block, p.Position, requestPos);
            _isRequest = true;

            p.SendSkillMotion(
                    type: SkillMotionType.Transform,
                    start: p.Position,
                    end: _midPos);


            MakeTargetPlayerStun();
            AddHitBox(p);
        }

        _elapsed += TimeUtil.Instance.DeltaTime;
        if (_elapsed > _duration)
        {
            ctx.RequestFinish();
        }

        _finalEnd = targetPos;

        return;
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

    public override bool CanCast(Player p, SkillContext ctx)
    {
        _target = ObjectManager.Instance.Find(ctx.TargetId);
        if (_target == null || !_target.IsAttackable() || /*!_target.IsUntargetable() || */
            (_target != null && Vector3.Distance(_target.Position, p.Position) > _dashDistance))
        {
            return false;
        }

        return true;
    }

    private void MakeTargetPlayerStun()
    {
        if (_target is Player targetPlayer)
        {
            StunStateDesc desc = new StunStateDesc();
            desc.EndPos = _target.Position;
            desc.Duration = _stunDuration;
            targetPlayer.ChangeState(new Player_StunState(desc));
        }
    }

    private void AddHitBox(Player p)
    {
        Vector2 hitPos = new Vector2(_target.Position.X, _target.Position.Z);
        p.Room.CollManager.AddHitbox(p, _characterType, _keyCode, hitPos);
    }
}

