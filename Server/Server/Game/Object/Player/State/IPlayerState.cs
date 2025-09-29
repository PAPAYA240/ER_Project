using Server.Game;
using System;
using System.Collections.Generic;
using System.Text;

public interface IPlayerState
{
    void Enter(Player player);
    void Execute(Player player);
    void Exit(Player player);  
}

