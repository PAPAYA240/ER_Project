using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using Server.Data;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

public class Player_RestState : IPlayerState
{
    bool _isRest = false;
    string _animName;
    private DateTime _startTime;
    private float _duration = 0.7f;

    public Player_RestState() {}

    public Player_RestState(bool isRest)
    {
        _isRest = isRest;
    }

    public void Enter(Player player)
    {
        _startTime = DateTime.UtcNow;

        player.SendStopPacket(StopReason.StopMoveOnly);

        if (_isRest == true)
        {
            _animName = "REST_START";
            player.IsHit = false;

            if (player.Info.Player.CharType == CharacterType.Abigail)
                player.Room.BroadcastAbigailSound(player, AbigailSound.Rest, 1f);
            else if (player.Info.Player.CharType == CharacterType.Yuki)
                player.Room.BroadcastAbigailSound(player, AbigailSound.YukiRest, 1f);
        }
        else
        {
            _animName = "REST_END";
        }

        player.SendAnimPacket(_animName, 0.1f);
        _duration = DataManager.AnimLengthInfoDict[player.Info.Player.CharType][_animName].Length;
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

        else if (player.IsHit == true)
        {
            player.IsHit = false;

            S_Rest restPkt = new S_Rest();
            restPkt.ObjectId = player.Id;
            restPkt.IsRest = false;
            player.SendRestPacket(restPkt);

            player.ChangeState(new Player_RestState(false));
            return;
        }
    }

    public void Exit(Player player)
    {
    }
}

