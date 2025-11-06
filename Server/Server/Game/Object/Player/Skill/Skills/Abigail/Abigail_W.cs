using System;
using System.Collections.Generic;
using System.Text;
using Google.Protobuf.Protocol;
using Server.Game;
using static Server.Data.DataUtils;


public sealed class Abigail_W : Skill_Abigail
{
    public Abigail_W()
    {
        _animName = "SKILL_W";
        _keyCode = KeyCode.W;
        _animDuration = GetDuration();
        StopSkillTime = 0.433f; // 12프레임 * 30FPS
    }

    public override void OnTick(Player p, SkillContext ctx)
    {
        if (CanStopSkill)
            return;

        float t = _elapsed / _animDuration;
        _elapsed += TimeUtil.DeltaTime;

        if(_elapsed >= StopSkillTime)
        {
            CanStopSkill = true;
            p.SendCanStopSkillPacket(CanStopSkill);
        }
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);
        // TODO: 코스트/쿨타임 차감

        SendSkillConfirmPacket(p);
        p.LookAtMouse(ctx.MousePos);
        p.SendCanStopSkillPacket(false);
    }
}
