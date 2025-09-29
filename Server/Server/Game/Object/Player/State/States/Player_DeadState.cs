using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Text;

public class Player_DeadState : IPlayerState
{
    public void Enter(Player player)
    {
        player.State = CreatureState.Dead;
        player.SendAnimPacket("DEAD", 0.1f);
    }

    public void Execute(Player player)
    {

    }

    public void Exit(Player player)
    {

    }
}

