using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Google.Protobuf.Protocol;
using Server.Game;
using static Server.Data.DataUtils;


public sealed class Abigail_E : SkillHandlerBase
{
    float _range = 6.2f;
    float _radius = 1.2f;
    GameObject _target = null;

    public Abigail_E()
    {
        _characterType = CharacterType.Abigail;
        _animName = "SKILL_E";
        _keyCode = KeyCode.E;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);
        // TODO: 코스트/쿨타임 차감

        p.SendSkillConfirmPacket(true, ctx.Key, VariantKey.NoCollision);

        p.PosInfo = new PositionInfo
        {
            State = p.PosInfo.State,
            PosX = ctx.MousePos.X,
            PosY = 0,
            PosZ = ctx.MousePos.Y
        };
        p.SendChangeTransformPacket(true);

        //_target = null;
    }

    public override bool CanCast(Player p, SkillContext ctx)
    {
        //if (_target != null)
        //    return false;

        float dist = p.PosInfo.Distance(ctx.MousePos);
        if (dist > _range + _radius)
            return false;

        GameObject target = p.Room.FindNearest(p.Id, ctx.MousePos, _radius);
        if (null == target)
            return false;

        //_target = target;
        return true;
    }
}
