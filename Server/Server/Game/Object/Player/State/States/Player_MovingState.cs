using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading;

public class Player_MovingState : IPlayerState
{
    private bool _isTargetOn = false;
    private int _targetId;
    private Vector3 _targetPos;

    private const int HUNDREDS_MS = 100;
    private const int THOUSANDS_MS = 1000;

    private long _nextCalcPathTick = 0;
    private long _nextMoveTick = 0;
    private long _nextWaitTick = Environment.TickCount64;

    public Player_MovingState(C_Move packet)
    {
        _isTargetOn = packet.IsTargetOn;
        _targetId = packet.TargetId;
        _targetPos = new Vector3
        {
            X = packet.TargetPosition.PosX,
            Y = packet.TargetPosition.PosY,
            Z = packet.TargetPosition.PosZ
        };

        _nextMoveTick = 0;
        _nextWaitTick = Environment.TickCount64;
        _nextCalcPathTick = Environment.TickCount64 + THOUSANDS_MS;
    }

    public void Enter(Player player)
    {
        player.State = CreatureState.Moving;
        player.SendAnimPacket("RUN", 0.1f);
        player.Get_CalculatePath(_targetPos);
    }

    public void Execute(Player player)
    {
        //if (_nextCalcPathTick < Environment.TickCount64)
        //{
        //    player.Get_CalculatePath(_targetPos);
        //    _nextCalcPathTick = Environment.TickCount64 + THOUSANDS_MS;
        //}

        if (_nextMoveTick > Environment.TickCount64)
            return;
        _nextMoveTick = Environment.TickCount64 + HUNDREDS_MS;

        if (player._path != null && player._path.Count > 0)
        {
            player.Get_MoveAlongPath();

            player.SendMovePacket(new PositionInfo(player.PosInfo), new RotationInfo(player.RotInfo));
        }

        if (_isTargetOn)
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
        _nextMoveTick = 0;
        _nextCalcPathTick = Environment.TickCount64 + THOUSANDS_MS;
    }
}

