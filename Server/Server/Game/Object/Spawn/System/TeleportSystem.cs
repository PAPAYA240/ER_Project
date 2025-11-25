using Google.Protobuf.Protocol;
using Lucene.Net.Index;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

public class TeleportSystem
{
    private readonly SpawnPointRegistry _registry;

    public TeleportSystem(SpawnPointRegistry registry)
    {
        _registry = registry;
    }

    public Vector3 GetTeleportPoint(Player player)
    {
        // 텔레포터 사용 가능 여부 먼저 체크 (쿨, 팀 제한 등)
        if (!IsTeleporterUsable(player, 0/*, packet.TeleporterId*/))
            return player.Position;

        bool enemySide = (player.Team == 1)
            ? false
            : true;

        var state = _registry.SelectSpawnPoint(
            enemySide,
            SpawnPointType.BushTeleport);

        if (state == null)
            return player.Position;

        return state.Info.Position;
    }

    private bool IsTeleporterUsable(Player player, int teleporterId)
    {
        // 서버에서 텔레포터 쿨타임 / 사용 조건 체크
        return true;
    }
}