using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static Server.Data.DataUtils;

public sealed class Hyunwoo_D : SkillHandlerBase
{
    private GameObject _target;

    private float _skillRange = 3.0f;
    private float _dashRange = 1.35f;
    private float _distanceToTarget;
    private float _moveDuration = 1f / 6f;
    private float _stateDuration = 1f / 3f;
    private float _elapsed;

    private Vector3 _dir;
    private Vector3 _startPos, _endPos;


    public Hyunwoo_D()
    {
        _characterType = CharacterType.Hyunwoo;
        _animName = "SKILL_D";
        _keyCode = KeyCode.D;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);

        // TODO Attack Speed based duration
        //_stateDuration = p.AttackSpeed;

        _dir = _target.Position - p.Position;
        _dir.Y = 0;
        _dir = Vector3.Normalize(_dir);

        _startPos = p.Position;

        _distanceToTarget = (_target.Position - p.Position).Length();

        // calculate End Position
        float approachDistance = (_skillRange - _dashRange);

        if (_distanceToTarget < approachDistance)
            _endPos = _startPos;
        else
        {
            _endPos = _startPos + _dir * (_distanceToTarget - approachDistance);
        }

        // Request collision position to client
        SendSkillCollisionRequestPacket(p, CollisionType.Block, p.Position, _endPos);

        p.Room.AttackSkillTarget(p, _target, _keyCode);
        p.SendSkillCostPacket(_keyCode);
        p.LookAtMouse(ctx.MousePos);

        if(p is Hyunwoo hyunwoo)
        {
            hyunwoo.AddTSkillCount(1);
        }
    }

    public override void OnHit(Player p, SkillContext ctx)
    {

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
            float t = Math.Clamp(_elapsed / _moveDuration, 0f, 1f);
            Vector3 targetPos = Vector3.Lerp(_startPos, _endPos, t);

            p.SendSkillMotion(
             type: SkillMotionType.Transform,
             start: p.Position,
             end: targetPos);

            _finalEnd = targetPos;

            _elapsed += TimeUtil.Instance.DeltaTime;
            if (_elapsed > _stateDuration)
            {
                p.ChangeState(new Hyunwoo_AttackState(_target.Id));

                //ctx.RequestFinish();
            }
        }
    }

    public override void OnExit(Player p, SkillContext ctx)
    {
        base.OnExit(p, ctx);
    }

    public override bool CanCast(Player p, SkillContext ctx)
    {
        _target = ObjectManager.Instance.Find(ctx.TargetId);
        if (_target == null || !_target.IsAttackable() || _target.IsUntargetable() || 
            (_target != null && Vector3.Distance(_target.Position, p.Position) > _skillRange))
        {
            return false;
        }

        return true;
    }
}

