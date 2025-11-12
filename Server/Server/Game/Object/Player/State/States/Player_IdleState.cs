using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Text;

public class Player_IdleState : IPlayerState
{
    public void Enter(Player player)
    {
        player.SendStopPacket(StopReason.StopMoveOnly);
        player.SendAnimPacket("WAIT", 0.1f);
    }

    public void Execute(Player player)
    {
    }

    public void Exit(Player player)
    {
    }
}

