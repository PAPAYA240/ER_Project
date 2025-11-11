using System;
using System.Collections.Generic;
using System.Text;
using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using Server.Data;
using Server.Game;


public class Player_OperateState : IPlayerState
{
    readonly string _animName = "OPERATE";
    private float _duration = 0.7f;
    private double _startTime;

    public void Enter(Player player)
    {
        player.State = CreatureState.Operate;
        player.SendStatePacket();
        player.SendAnimPacket(_animName, 0.1f);

        _duration = DataManager.AnimLengthInfoDict[player.Info.Player.CharType][_animName].Length;
        _startTime = TimeUtil.UtcSec();
        player.SendStopPacket(StopReason.StopMoveOnly);

        S_RotateToPos rotateToPosPkt = new S_RotateToPos();
        rotateToPosPkt.ObjectId = player.Id;
        rotateToPosPkt.PosX = player.Room.BeaconManager.GetBeaconPos(player.Beacon).X;
        rotateToPosPkt.PosZ = player.Room.BeaconManager.GetBeaconPos(player.Beacon).Z;
        player.Room.Push(player.Room.Broadcast, rotateToPosPkt);

        player.Room.BeaconManager.Operate(player);
    }

    public void Execute(Player player)
    {
        bool animFinished = _startTime + _duration <= TimeUtil.UtcSec();
        bool canOccupy = player.Room.BeaconManager.IsOccupiable(player.Team, player.Beacon);

        if (false == canOccupy || animFinished)
            player.ChangeState(new Player_IdleState());

        if (animFinished)
            player.Room.BeaconManager.OccupyBeacon(player);
    }

    public void Exit(Player player)
    {
        player.Room.BeaconManager.ExitOperate(player);
    }
}

