using Server.Game;
using System;
using System.Collections.Generic;
using System.Text;

public interface IRegenEffect
{
    bool IsActive { get; }
    void OnTick(Player owner);
}

