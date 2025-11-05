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
    }

    public void Execute(Player player)
    {
        if(_startTime + _duration <= TimeUtil.UtcSec())
            player.ChangeState(new Player_IdleState());
    }

    public void Exit(Player player)
    {
    }
}

