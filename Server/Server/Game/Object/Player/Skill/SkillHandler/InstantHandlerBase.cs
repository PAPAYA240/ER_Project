using Server.Data;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Text;

public abstract class InstantHandlerBase : SkillHandlerBase
{
    public virtual void ExecuteInstant(Player p) { }
}

