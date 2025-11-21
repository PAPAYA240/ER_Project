using Google.Protobuf.Protocol;
using Server.Game;
using static Server.Data.DataUtils;

public abstract class RozziSkillHandler : SkillHandlerBase
{
    protected void AddAttackToken(Player p)
    {
        if (p.PeekToken(InputKind.Attack) != null)
            return;

        p.Tokens.Add(new NextInputToken
        {
            Active = true,
            RemainingUses = 1,
            ExpireUtc = TimeUtil.UtcSec() + 4.0,
            Priority = 10,
            Trigger = InputKind.Attack,
            CancelOnUseSkill = false,
        });
    }
}
