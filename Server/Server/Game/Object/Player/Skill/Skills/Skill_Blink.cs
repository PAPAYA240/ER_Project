using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Game;
using System.Numerics;
using static Server.Data.DataUtils;

public sealed class Skill_Blink : SkillHandlerBase
{
    private float _blinkDistance = 3.0f;

    public Skill_Blink()
    {
        _keyCode = KeyCode.F;
        HitboxRequired = false;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        p.SendStopPacket(StopReason.StopMoveOnly);
        p.SendSkillCostPacket(_keyCode);

        Vector3 mousePos = new Vector3(ctx.MousePos.X, p.Position.Y, ctx.MousePos.Y);
        Vector3 dir = Vector3.Normalize(mousePos - p.Position);

        float distance =  Vector3.Distance(mousePos, p.Position);
        Vector3 targetPos = p.Position;
        if (distance < _blinkDistance)
            targetPos = mousePos;
        else
            targetPos = p.Position + dir * _blinkDistance;

        SendSkillCollisionRequestPacket(p, CollisionType.Pass, p.Position, targetPos);

        p.SendCommonSkillEffect(ctx.MousePos, commonName: "Blink", type: "Caster");
        p.SendSoundPacket("Blink");
    }

    public override void OnTick(Player p, SkillContext ctx)
    {
        if (_requestId != _commitId)
        {
            if (TryConsumeLatest(ref _commitId, out SkillCollisionProposal prop))
            {
                Vector2 targetPos = new Vector2(prop.collisionPos.X, prop.collisionPos.Z);
                p.LookAtMouse(targetPos);

                p.SendSkillMotion(
                    type: SkillMotionType.Transform,
                    start: p.Position,
                    end: prop.collisionPos,
                    authoritativeEnd: true);

                p.PosInfo.MergeFrom(prop.collisionPos.ToPositionInfo());
                p.SendChangeTransformPacket(isWarp: true);

                p.SendCommonSkillEffect(targetPos, commonName: "Blink", type: "Select", fxName: "FX_BI_Blink_Swift");
                p.SendCommonSkillEffect(targetPos, commonName: "Blink", type: "Select", fxName: "FX_BI_Blink_End");

                ctx.RequestFinish();
                return;
            }
        }
        else
            return;
    }

    public override void OnExit(Player p, SkillContext ctx)
    {
        base.OnExit(p, ctx);
        p.SendRemoveCommonEffect(isCaster: false, commonName: "Blink", fxName: "FX_BI_Blink_End");
    }
}

