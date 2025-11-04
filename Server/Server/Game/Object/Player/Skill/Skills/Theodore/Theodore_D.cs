using Google.Protobuf.Protocol;
using Server.Game;
using static Server.Data.DataUtils;

public sealed class Theodore_D : SkillHandlerBase
{
    public override bool CanMoveDuringCast => false;
    public override float MoveSpeedMultiplier => 1.2f;

    private const string ANIM_START = "SKILL_D_START";
    private const string ANIM_SKILL = "SKILL_D";
    private const string ANIM_END = "SKILL_D_END";
    public Theodore_D()
    {
        _characterType = CharacterType.Theodore;
        _animName = ANIM_START;
        _keyCode = KeyCode.D;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);
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

