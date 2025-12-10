using System;
using System.Collections.Generic;
using System.Text;
using Google.Protobuf.Protocol;
using Server.Game;
using static Server.Data.DataUtils;


public sealed class Abigail_W : Skill_Abigail
{
    bool _attackPktSent = false;

    public Abigail_W()
    {
        _animName = "SKILL_W";
        _keyCode = KeyCode.W;
        _animDuration = GetDuration();
        StopSkillTime = 0.5f;
    }

    public override void OnTick(Player p, SkillContext ctx)
    {
        _elapsed += TimeUtil.Instance.DeltaTime;

        if(!CanStopSkill && _elapsed >= StopSkillTime)
        {
            CanStopSkill = true;
            p.SendCanStopSkillPacket(CanStopSkill);
        }

        if(!_attackPktSent && _elapsed >= TimeUtil.FrameToSec(7))
        {
            _attackPktSent = true;
            p.Room.Push(p.Room.BroadcastAbigailFx, p, AbigailFx.WAttack, 0f);
        }
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);
        // TODO: 코스트/쿨타임 차감

        SendSkillConfirmPacket(p);
        p.SendStopPacket();
        p.LookAtMouse(ctx.MousePos);
        p.SendCanStopSkillPacket(false);

        p.Room.BroadcastAbigailSound(p, AbigailSound.W, 1);
        p.Room.BroadcastAbigailSound(p, AbigailSound.Wvoice, 0.6f);

        p.Room.BroadcastAbigailFx(p, AbigailFx.WRange, 0.5f);
    }

    public override void OnExit(Player p, SkillContext ctx)
    {
        p.Room.BroadcastStopAbglFx(p, AbigailFx.WRange);
        p.Room.BroadcastStopAbglFx(p, AbigailFx.WAttack);
    }
}
