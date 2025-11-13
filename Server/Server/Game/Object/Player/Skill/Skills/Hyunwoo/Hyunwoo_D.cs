using Google.Protobuf.Protocol;
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
    public Hyunwoo_D()
    {
        _characterType = CharacterType.Hyunwoo;
        _animName = "SKILL_D";
        _keyCode = KeyCode.D;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);

        SendSkillConfirmPacket(p);
        p.LookAtMouse(ctx.MousePos);
    }

    public override void OnHit(Player p, SkillContext ctx)
    {

    }

    public override void OnTick(Player p, SkillContext ctx)
    {

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

