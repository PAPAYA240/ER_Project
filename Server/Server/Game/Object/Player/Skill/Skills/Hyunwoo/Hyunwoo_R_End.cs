using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static Player_StunState;
using static Server.Data.DataUtils;


public sealed class Hyunwoo_R_End : SkillHandlerBase
{
    float _knockbackRange = 1.5f;
    Dictionary<int, Player> _players = new Dictionary<int, Player>();
    public Hyunwoo_R_End()
    {
        _characterType = CharacterType.Hyunwoo;
        _animName = "SKILL_R_END";
        _keyCode = KeyCode.R;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);

        SendSkillConfirmPacket(p);
        p.LookAtMouse(ctx.MousePos);
    }

    public override void OnHit(Player p, SkillContext ctx)
    {

    }

    public override void OnCollision<T>(Player p, List<T> targets, GameObject.StatusEffect effect)
    {
        foreach (var targetObject in targets)
        {
            if (targetObject is Player targetPlayer)
            {

                Player_SkillState skillstate = p.CurrentState as Player_SkillState;
                if (skillstate != null)
                {
                    Vector3 targetPos = targetPlayer.Position;
                    Vector3 dir = (new Vector3(skillstate.Ctx.MousePos.X, 0, skillstate.Ctx.MousePos.Y) - p.Position);
                    Vector3 endPos = targetPos + Vector3.Normalize(dir) * _knockbackRange;

                    SendSkillCollisionRequestPacket(p, CollisionType.Block, targetPos, endPos);
                    _players.Add(_requestId, targetPlayer);
                }
            }
        }
    }

    public override void OnTick(Player p, SkillContext ctx)
    {
        if (_requestId != _commitId)
        {
            if (TryConsumeLatest(ref _commitId, out SkillCollisionProposal prop))
            {
                if (_players.TryGetValue(_commitId, out Player tartget))
                {
                    StunStateDesc desc = new StunStateDesc();
                    desc.EndPos = prop.collisionPos;
                    desc.Duration = 0.1f;
                    desc.Speed = 30f;
                    tartget.ChangeState(new Player_StunState(desc));
                }
            }
        }
    }

    public override void OnExit(Player p, SkillContext ctx)
    {
        base.OnExit(p, ctx);

    }
}

