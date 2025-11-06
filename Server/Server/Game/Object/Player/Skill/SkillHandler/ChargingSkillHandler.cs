using Google.Protobuf.Protocol;
using Server.Game;
using static Server.Data.DataUtils;

public abstract class ChargingSkillHandler : SkillHandlerBase
{
    public override void OnEnter(Player p, SkillContext ctx)
    {
        LastSeq = 0;
        Latest = default;
        _committed = false;

        // 애니메이션 패킷 전송
        p.SendAnimPacket(_animName, 0.05f);

        // 이동 잠금
        if (CanMoveDuringCast == false)
        {
            // 이동 금지 스킬이면 강제 정지
            p.SendStopPacket(StopReason.StopMoveOnly);
        }

        //switch (ctx.Key)
        //{
        //    case KeyCode.Q:
        //    case KeyCode.W:
        //    case KeyCode.E:
        //    case KeyCode.R:
        //        p.Room.CollManager.AddHitbox(p, p.Info.Player.CharType, ctx.Key, ctx.MousePos);
        //        break;

        //}
    }

    public virtual void OnCharge(Player p, SkillContext ctx)
    {

    }
}
