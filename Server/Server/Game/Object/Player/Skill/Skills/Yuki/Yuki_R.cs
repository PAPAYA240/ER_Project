using Google.Protobuf.Protocol;
using Server.Game;
using static Server.Data.DataUtils;

public sealed class Yuki_R : SkillHandlerBase
{
    public Yuki_R()
    {
        _characterType = CharacterType.Yuki;
        _animName = "SKILL_R";
        _keyCode = KeyCode.R;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);
        // TODO: 코스트/쿨타임 차감

        SendSkillConfirmPacket(p);
        p.LookAtMouse(ctx.MousePos);

        p.SendYukiSkillEffect(SkillEffectType.RRange);

        p.Room.Push(p.Room.BroadcastAbigailSound, p, AbigailSound.YukiRactive, 1f);
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
