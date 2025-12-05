using System;
using System.Collections.Generic;
using System.Text;
using Google.Protobuf.Protocol;
using Server.Game;
using static Server.Data.DataUtils;


public sealed class Abigail_R : Skill_Abigail
{
    bool _trailPktSent = false;
    bool _explodePktSent = false;

    public Abigail_R()  
    {
        _animName = "SKILL_R";
        _keyCode = KeyCode.R;
        _animDuration = GetDuration();
        StopSkillTime = 0.733f; // 26프레임 * 30FPS
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);
        // TODO: 코스트/쿨타임 차감

        SendSkillConfirmPacket(p);
        p.SendCanStopSkillPacket(false);
        p.Room.AddStatusEffect(p, p, _keyCode, null); // 지정불가

        p.Room.BroadcastAbigailSound(p, AbigailSound.R, 1f);
        p.Room.BroadcastAbigailSound(p, AbigailSound.Rvoice, 1f);

        p.Room.BroadcastAbigailFx(p, AbigailFx.RRange, 0.5f);
        p.Room.BroadcastAbigailFx(p, AbigailFx.RStart, 0);
    }

    public override void OnTick(Player p, SkillContext ctx)
    {
        _elapsed += TimeUtil.Instance.DeltaTime;

        if (!CanStopSkill && _elapsed >= StopSkillTime)
        {
            CanStopSkill = true;
            p.SendCanStopSkillPacket(CanStopSkill);
        }

        if(!_trailPktSent && _elapsed >= TimeUtil.FrameToSec(15))
        {
            _trailPktSent = true;
            p.Room.Push(p.Room.BroadcastAbigailFx, p, AbigailFx.RTrail, 0f);
        }

        if(!_explodePktSent && _elapsed >= TimeUtil.FrameToSec(21))
        {
            _explodePktSent = true;
            p.Room.Push(p.Room.BroadcastAbigailFx, p, AbigailFx.RExplode, 0f);
        }
    }

    public override void OnExit(Player p, SkillContext ctx)
    {
        p.Room.BroadcastStopAbglFx(p, AbigailFx.RRange);
    }
}
