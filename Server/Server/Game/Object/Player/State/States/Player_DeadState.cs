using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game;
using ServerCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static Lucene.Net.Util.AttributeSource;

public class Player_DeadState : IPlayerState
{
    public void Enter(Player player)
    {
        player.SendAnimPacket("DEAD", 0.1f);

        //UI

        RespawnTime(player);
    }

    public void Execute(Player player)
    {

    }

    public void Exit(Player player)
    {

    }

    void RespawnTime(Player player)
    {
        S_Die diePacket = new S_Die();
        diePacket.ObjectId = player.Id;
        diePacket.AttackerId = player.GetLastAttackerId();

        diePacket.RespawnTime = DataManager.RespawnDict[player.Stat.Level];
        _ = CoRespawnTime(player, diePacket.RespawnTime, respawnAtZero: false);

        player.Room.Broadcast(diePacket);
    }

    private async Task CoRespawnTime(Player player, float respawnTime, bool respawnAtZero = true)
    {
        var sw = Stopwatch.StartNew();

        while (sw.Elapsed.TotalSeconds < respawnTime)
        {
            await Task.Delay(10); // 0.01초마다 남은 쿨타임 갱신
        }

        if (player.Room == null)
            return;

        S_Respawn respawnPacket = new S_Respawn();
        respawnPacket.ObjectId = player.Id;
        respawnPacket.IsRest = false;

        respawnPacket.Hp = player.Hp = player.MaxHp;
        respawnPacket.Stamina = player.Stamina = player.MaxStamina;

        if (true == respawnAtZero)
        {
            respawnPacket.PosInfo = new PositionInfo
            {
                PosX = 0,
                PosY = 0,
                PosZ = 0
            };
            respawnPacket.RotInfo = new RotationInfo
            {
                Qx = 0,
                Qy = 0,
                Qz = 0,
                Qw = 1
            };

            player.Info.PosInfo = new PositionInfo(respawnPacket.PosInfo);
            player.Info.RotInfo = new RotationInfo(respawnPacket.RotInfo);
        }
        else
        {
            respawnPacket.PosInfo = player.Room.Spawn.GetSpawnPoint(player.Team).ToPositionInfo();
            respawnPacket.RotInfo = player.Info.RotInfo;
        }

        player.SendDeadPacket(respawnPacket);

        player.ChangeState(new Player_IdleState());
    }
}

