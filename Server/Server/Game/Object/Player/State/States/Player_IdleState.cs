using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Text;

public class Player_IdleState : IPlayerState
{
    private long _nextScanTick;

    public void Enter(Player player)
    {
        player.State = CreatureState.Idle;
        player.SendAnimPacket("WAIT", 0.1f);
        _nextScanTick = Environment.TickCount64;
    }

    public void Execute(Player player)
    {
        long now = Environment.TickCount64;
        if (now < _nextScanTick)
            return;
        _nextScanTick = now + 250; // ms

        var enemy = player.FindNearestEnemy(player.AttackRange);
        if (enemy != null)
            player.ChangeState(new Player_AttackState(enemy.Id, chaseAllowed: true));
    }

    public void Exit(Player player)
    {

    }
}

