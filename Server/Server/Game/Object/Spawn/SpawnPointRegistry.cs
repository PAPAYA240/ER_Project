using Google.Protobuf.Protocol;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

public class SpawnPointRegistry
{
    private readonly List<SpawnPointState> _points = new();
    private readonly double _cooldownSec = 5000;

    public SpawnPointRegistry(double spawnCooldownSec)
    {
        _cooldownSec = spawnCooldownSec;
    }

    public bool IsSpawnDataLoaded() => _points.Any();
    public void Clear() => _points.Clear();

    public void Add(SpawnPointInfo info)
    {
        _points.Add(new SpawnPointState
        {
            Info = info,
            LastUsedTimeSec = double.NegativeInfinity,
            AvailableAtSec = 0
        });
    }

    /// <summary>
    /// side + type 으로 필터해서 스폰 포인트 선택
    /// 전부 쿨타임이면 가장 먼저 사용되었던 포인트를 반환
    /// </summary>
    public SpawnPointState SelectSpawnPoint(
        bool side,
        SpawnPointType type)
    {
        double nowSec = TimeUtil.Instance.LastTick;

        var candidates = _points
            .Where(p => p.Info.Team == side && p.Info.Type == type)
            .ToList();

        if (candidates.Count == 0)
            return null;

        // 1) 지금 사용 가능한 애들
        var available = candidates
            .Where(p => p.IsAvailable(nowSec))
            .ToList();

        SpawnPointState selected;

        if (available.Count > 0)
        {
            // 사용 가능한 것들 중에서 랜덤
            int idx = Random.Shared.Next(0, available.Count);
            selected = available[idx];
        }
        else
        {
            // 2) 전부 쿨타임이면: 가장 먼저 사용되었던 애 선택
            // = LastUsedTimeSec 가 가장 오래된 것
            selected = candidates
                .OrderBy(p => p.LastUsedTimeSec)
                .First();
        }

        // 선택된 포인트 쿨타임 갱신
        selected.LastUsedTimeSec = nowSec;
        selected.AvailableAtSec = nowSec + _cooldownSec;

        return selected;
    }
}