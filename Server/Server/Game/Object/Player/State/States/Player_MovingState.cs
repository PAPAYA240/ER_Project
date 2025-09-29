using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

public class Player_MovingState : IPlayerState
{
    private bool _isTargetOn = false;   
    private Vector3 _targetPos;

    public Player_MovingState(C_Move packet)
    {
        _isTargetOn = packet.IsTargetOn;
        _targetPos = new Vector3
        {
            X = packet.TargetPosition.PosX,
            Y = packet.TargetPosition.PosY,
            Z = packet.TargetPosition.PosZ
        };
    }

    public void Enter(Player player)
    {
        player.State = CreatureState.Moving;
        player.SendAnimPacket("RUN", 0.1f);
    }

    public void Execute(Player player)
    {
        if(_isTargetOn)
        {
            // 사거리 안에 들어가면 Attack으로 전환
            float attackRange = 3.0f;   // TEMP : 무기별 실제 사거리 가져와야 함
            if (Vector3.Distance(player.Position, _targetPos) <= attackRange)
                player.ChangeState(new Player_AttackState());
        }
        else
        {
            float stopRange = 0.2f;
            if (Vector3.Distance(player.Position, _targetPos) <= stopRange)
                player.ChangeState(new Player_IdleState());
        }
    }

    public void Exit(Player player)
    {

    }
}

