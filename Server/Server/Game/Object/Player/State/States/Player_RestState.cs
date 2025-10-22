using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Text;

public class Player_RestState : IPlayerState
{
    bool _isRest = false;
    private DateTime _startTime;
    private float _duration = 0.7f;

    public Player_RestState(bool isRest)
    {
        _isRest = isRest;
    }

    public void Enter(Player player)
    {
        player.State = CreatureState.Rest;
        player.SendStatePacket();

        _startTime = DateTime.UtcNow;

        if (_isRest == true)
            player.SendAnimPacket("REST_START", 0.1f);
        else
        {
            player.SendAnimPacket("REST_END", 0.1f);
        }
    }

    public void Execute(Player player)
    {
        if (_isRest == false)
        {
            // 현재 시각에서 경과 시간 계산
            double elapsed = (DateTime.UtcNow - _startTime).TotalSeconds;

            if (elapsed >= _duration)
            {
                player.ChangeState(new Player_IdleState());
            }
        }
    }

    public void Exit(Player player)
    {
    }
}

