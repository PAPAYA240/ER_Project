using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Google.Protobuf.Protocol;
using Server.Game;
using ServerCore;
using static Server.Data.DataUtils;


public sealed class Abigail_Q : Skill_Abigail
{
    public override bool CanMoveDuringCast => true;
    bool _secondFxSent = false;
    
    public Abigail_Q()
    {
        _animName = "SKILL_Q";
        _keyCode = KeyCode.Q;
        _animDuration = GetDuration();
        StopSkillTime = TimeUtil.FrameToSec(17);
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);
        // TODO: 코스트/쿨타임 차감

        SendSkillConfirmPacket(p);
        p.Room.AddStatusEffect(p, p, _keyCode, null); // 스킬 사용시 이속 버프

        p.Room.BroadcastAbigailSound(p, AbigailSound.Q, 1);
        p.Room.BroadcastAbigailSound(p, AbigailSound.Qvoice, 0.6f);

        //p.Room.BroadcastAbigailFx(p, AbigailFx.QAttack, TimeUtil.FrameToSec(7));
        p.Room.BroadcastAbigailFx(p, AbigailFx.QRange, _animDuration);
    }

    public override void OnTick(Player p, SkillContext ctx)
    {
        _elapsed += TimeUtil.Instance.DeltaTime;

        if (!_secondFxSent && _elapsed >= TimeUtil.FrameToSec(10))
        {
            _secondFxSent = true;
            //p.Room.Push(p.Room.BroadcastAbigailFx, p, AbigailFx.QAttack2, TimeUtil.FrameToSec(6));
        }

        if (!CanStopSkill && _elapsed >= StopSkillTime)
        {
            CanStopSkill = true;
            p.SendCanStopSkillPacket(CanStopSkill);
        }
    }

    public override void OnExit(Player p, SkillContext ctx)
    {
        p.Room.BroadcastStopAbglFx(p, AbigailFx.QAttack);
        p.Room.BroadcastStopAbglFx(p, AbigailFx.QRange);
        p.Room.BroadcastStopAbglFx(p, AbigailFx.QAttack2);
    }
}
