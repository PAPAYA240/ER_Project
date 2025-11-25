using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

public struct SpawnPointInfo
{
    public int Id;
    public bool Team;
    public SpawnPointType Type;
    public Vector3 Position;
}

public class SpawnPointState
{
    public SpawnPointInfo Info;
    public double LastUsedTimeSec;      // 마지막 사용 시간(초) – TimeUtil.UtcSec() 같은 것 사용
    public double AvailableAtSec;       // 이 시간 이후에 다시 사용 가능

    public bool IsAvailable(double nowSec) => nowSec >= AvailableAtSec;
}