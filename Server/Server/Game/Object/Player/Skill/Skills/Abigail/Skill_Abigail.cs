using System;
using System.Collections.Generic;
using System.Text;
using Google.Protobuf.Protocol;
using Server.Game;
using static Server.Data.DataUtils;


public class Skill_Abigail : SkillHandlerBase
{
    protected float _elapsed;
    protected float _animDuration;
    protected float StopSkillTime { get; set; } = float.MaxValue;

    public Skill_Abigail()
    {
        _characterType = CharacterType.Abigail;
    }

    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);
    }
}
