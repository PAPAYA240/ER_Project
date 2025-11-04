using Google.Protobuf.Protocol;
using Server.Game;
using static Server.Data.DataUtils;

public sealed class Theodore_Q : SkillHandlerBase
{
    public override bool CanMoveDuringCast => false;
    public Theodore_Q()
    {
        _characterType = CharacterType.Theodore;
        _animName = "SKILL_Q";
        _keyCode = KeyCode.Q;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);

        p.LookAtMouse(ctx.MousePos);
        p.SendSkillConfirmPacket(true, ctx.Key, VariantKey.NoCollision);
    }

    public override void OnHit(Player p, SkillContext ctx)
    {
        return;
    }

    // TEMP
    bool isCommited = false;
    public override void OnTick(Player p, SkillContext ctx)
    {
        if (isCommited)
            return;

        isCommited = true;  

        return;
    }

    public override void OnExit(Player p, SkillContext ctx)
    {
        base.OnExit(p, ctx);
    }
}

