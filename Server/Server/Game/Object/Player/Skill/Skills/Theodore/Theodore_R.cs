using Google.Protobuf.Protocol;
using Server.Game;
using static Server.Data.DataUtils;

public sealed class Theodore_R : SkillHandlerBase
{
    public override bool CanMoveDuringCast => false;
    public Theodore_R()
    {
        _characterType = CharacterType.Theodore;
        _animName = "SKILL_R";
        _keyCode = KeyCode.R;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);

        p.SendSkillConfirmPacket(true, ctx.Key, VariantKey.NoCollision);
        p.LookAtMouse(ctx.MousePos);
    }

    public override void OnHit(Player p, SkillContext ctx)
    {
        return;
    }

    public override void OnTick(Player p, SkillContext ctx)
    {
        return;
    }

    public override void OnExit(Player p, SkillContext ctx)
    {
        base.OnExit(p, ctx);
    }
}

