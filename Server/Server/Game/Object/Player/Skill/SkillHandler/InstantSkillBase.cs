using Server.Data;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Text;

public abstract class InstantSkillBase : ISkill
{
    public void OnCollision(Player p)
    {
        throw new NotImplementedException();
    }

    public void OnEnter(Player p, SkillContext ctx)
    {
        throw new NotImplementedException();
    }

    public void OnExit(Player p, SkillContext ctx)
    {
        throw new NotImplementedException();
    }

    public void OnHit(Player p, SkillContext ctx)
    {
        throw new NotImplementedException();
    }

    public void OnMove(Player p)
    {
        throw new NotImplementedException();
    }

    public void OnPropose(Player p, in SkillCollisionProposal prop)
    {
        throw new NotImplementedException();
    }

    public void OnStop(Player p)
    {
        throw new NotImplementedException();
    }

    public void OnTick(Player p, SkillContext ctx)
    {
        throw new NotImplementedException();
    }

    public bool CanMoveDuringCast => throw new NotImplementedException();

    public float MoveSpeedMultiplier => throw new NotImplementedException();

    public bool CanStopSkill => throw new NotImplementedException();

    public bool CanCast(Player p, SkillContext ctx)
    {
        throw new NotImplementedException();
    }

    public float GetDuration()
    {
        throw new NotImplementedException();
    }

    public DataUtils.KeyCode GetKeyCode()
    {
        throw new NotImplementedException();
    }
}

