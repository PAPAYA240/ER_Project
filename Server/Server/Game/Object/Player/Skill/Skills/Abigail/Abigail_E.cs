using Server.Game;
using static Server.Data.DataUtils;


public sealed class Abigail_E : Skill_Abigail
{
    float _range = 6.2f;
    float _radius = 1.2f;
    GameObject _target = null;

    public Abigail_E()
    {
        _animName = "SKILL_E";
        _keyCode = KeyCode.E;
        _animDuration = GetDuration();
        HitboxCreated = false;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);
        
        SendSkillConfirmPacket(p);

        p.PosInfo.PosX = ctx.MousePos.X;
        p.PosInfo.PosZ = ctx.MousePos.Y;
        p.SendChangeTransformPacket(true);
        p.Room.AttackSkillTarget(p, _target, _keyCode);
        CanStopSkill = true;
    }

    public override bool CanCast(Player p, SkillContext ctx)
    {
        float dist = p.Info.PosInfo.Distance(ctx.MousePos);
        if (dist > _range + _radius)
            return false;

        GameObject target = p.Room.FindNearest(p.Id, ctx.MousePos, _radius);
        if (null == target)
            return false;

        _target = target;
        return true;
    }
}
