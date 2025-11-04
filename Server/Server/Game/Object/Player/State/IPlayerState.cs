using Google.Protobuf.Protocol;
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

public interface IReceivesMoveCommand
{
    void OnMoveCommand(Player player, C_Move cmd);
}
public interface IReceivesStopCommand
{
    void OnStopCommand(Player player, C_Stop stopPacket);
}

public interface IReceivesAttackCommand
{
    public bool IsSwingActive();

    public void RequestTargetChange(int targetId);
}