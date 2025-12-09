using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using static Server.Game.GameObject;

public class Player_DeadState : IPlayerState
{
    public void Enter(Player player)
    {
        player.SendAnimPacket("DEAD", 0.1f);
        player.SendStopPacket(StopReason.StopMoveOnly);

        player.RemoveAllStatusEffects();

        RespawnTime(player);

        if(player.Info.Player.CharType == CharacterType.Abigail)
            player.Room.BroadcastAbigailSound(player, AbigailSound.Dead, 1);
    }

    public void Execute(Player player)
    {
        if (TimeUtil.Instance.IsPastOrNow(player.DeadRespawnEndTick))
        {
            DoRespawn(player);
            return;
        }
    }

    public void Exit(Player player)
    {
        StatusEffect se = new StatusEffect();
        se.type = "Untargetable";
        se.duration = 1f;
        player.AddStatusEffect(se);
    }

    void RespawnTime(Player player)
    {
        S_Die diePacket = new S_Die();
        diePacket.ObjectId = player.Id;
        diePacket.AttackerId = player.GetLastAttackerId();

        float respawnSec = DataManager.RespawnDict[player.Stat.Level];
        diePacket.RespawnTime = respawnSec;

        long start = TimeUtil.Instance.LastTick;
        long end = unchecked(start + (int)(respawnSec * 1000));
        player.DeadRespawnEndTick = end;

        player.Room.Push(player.Room.Broadcast, diePacket);
    }

    private void DoRespawn(Player player)
    {
        if (player.Room == null)
            return;

        S_Respawn respawnPacket = new S_Respawn();
        respawnPacket.ObjectId = player.Id;
        respawnPacket.IsRest = false;

        respawnPacket.Hp = player.Hp = player.MaxHp;
        respawnPacket.Stamina = player.Stamina = player.MaxStamina;

        // 스폰 포인트
        respawnPacket.PosInfo = player.Room.Spawn.GetSpawnPoint(player.Team).ToPositionInfo();
        respawnPacket.RotInfo = player.Info.RotInfo;

        player.SendDeadPacket(respawnPacket);

        player.ChangeState(new Player_IdleState());
    }
}

