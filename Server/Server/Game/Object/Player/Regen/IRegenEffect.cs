using Server.Game;
using System;
using System.Collections.Generic;
using System.Text;

public interface IRegenEffect
{
    public enum StatRegenType
    {
        None = 0,
        BaseRegen = 1,
        RestRegen = 2,
        BaseAreaRegen = 3,
    }

    StatRegenType Effect { get; }
    bool IsActive { get; }
    void OnTick(Player owner);
}

