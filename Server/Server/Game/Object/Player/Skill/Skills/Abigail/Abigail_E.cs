using System.Numerics;
using Google.Protobuf.Protocol;
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
        HitboxRequired = false;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);

        p.SendStopPacket(StopReason.StopMoveOnly);
        SendSkillConfirmPacket(p);

        Vector3 mousePos = new Vector3(ctx.MousePos.X, p.Position.Y, ctx.MousePos.Y);

        SendSkillCollisionRequestPacket(p, CollisionType.Pass, p.Position, mousePos);

        p.Room.BroadcastAbigailSound(p, AbigailSound.E, 1);
        p.Room.BroadcastAbigailSound(p, AbigailSound.Evoice, 0.6f);
        p.Room.BroadcastAbigailSound(p, AbigailSound.Ehit, 1);
    }

    public override void OnTick(Player p, SkillContext ctx)
    {
        if (_requestId != _commitId)
        {
            if (TryConsumeLatest(ref _commitId, out SkillCollisionProposal prop))
            {
                Vector2 targetPos = new Vector2(prop.collisionPos.X, prop.collisionPos.Z);

                p.Room.Push(p.Room.BroadcastAbglPortal, p, new Vector2(p.PosInfo.PosX, p.PosInfo.PosZ), targetPos);
                p.Room.Push(p.Room.AttackSkillTarget, p, _target, _keyCode);

                p.PosInfo.PosX = targetPos.X;
                p.PosInfo.PosZ = targetPos.Y;
                p.SendChangeTransformPacket(true);
                CanStopSkill = true;
            }
        }
    }

    public override bool CanCast(Player p, SkillContext ctx)
    {
        float dist = p.Info.PosInfo.Distance(ctx.MousePos);
        if (dist > _range + _radius)
            return false;

        GameObject target = p.Room.FindNearestEnemy(p.Info.Player.Team, p.Id, ctx.MousePos, _radius);
        if (null == target)
            return false;

        _target = target;
        return true;
    }
}
