using Google.Protobuf.Protocol;
using Microsoft.VisualBasic;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

public class SpawnSystem
{
    private readonly SpawnPointRegistry _registry;

    public SpawnSystem(SpawnPointRegistry registry)
    {
        _registry = registry;
    }

    public Vector3 GetSpawnPoint(int team)
    {
        bool myTeam = (team == 1)
            ? true
            : false;

        var state = _registry.SelectSpawnPoint(
            myTeam,
            SpawnPointType.BaseSpawn);

        if (state == null)
            return Vector3.Zero;

        return state.Info.Position;
    }
}
