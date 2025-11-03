using System;
using System.Collections.Generic;
using System.Text;
using Google.Protobuf.Protocol;
using Server.Game;
using static Server.Data.DataUtils;


public sealed class Abigail_R : Skill_Abigail
{
    public Abigail_R()  
    {
        _animName = "SKILL_R";
        _keyCode = KeyCode.R;
        _animDuration = GetDuration();
    }
    public override void OnTick(Player p, SkillContext ctx)
    {
        float t = _elapsed / _animDuration;
        _elapsed += TimeUtil.DeltaTime;
        
        CanStopSkill = true;
        p.SendCanStopSkillPacket(CanStopSkill);
        return;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);
        // TODO: 코스트/쿨타임 차감

        p.SendSkillConfirmPacket(true, ctx.Key, VariantKey.NoCollision);
    }
}
