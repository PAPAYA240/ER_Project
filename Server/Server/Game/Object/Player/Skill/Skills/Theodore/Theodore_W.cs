using Google.Protobuf.Protocol;
using Server.Game;
using System.Numerics;
using static Server.Data.DataUtils;

public sealed class Theodore_W : SkillHandlerBase
{
    public override bool CanMoveDuringCast => true;

    public Theodore_W()
    {
        _characterType = CharacterType.Theodore;
        _animName = "SKILL_W";
        _keyCode = KeyCode.W;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);

        // 패킷이 마우스보다 수락 패킷이 먼저 도착함
        // 그래서 회전 전에 이펙트가 먼저 호출되는 문제가 생겨
        // confirm에 isLookatMouse 플래그를 추가
        SendSkillConfirmPacket(p, true);
        p.SendSkillEffect(ctx.MousePos, keyCode: _keyCode, sendLookatMousePacket: true);
    }

    public override void OnHit(Player p, SkillContext ctx)
    {
        return;
    }
    public override void OnTick(Player p, SkillContext ctx)
    {

    }

    public override void OnExit(Player p, SkillContext ctx)
    {
        base.OnExit(p, ctx);
    }

    private void CommitMotionOnce(Player p, Vector3 from, Vector3 end)
    {
       
    }
}

