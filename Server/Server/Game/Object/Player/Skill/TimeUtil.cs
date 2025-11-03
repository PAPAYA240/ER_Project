using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

public static class TimeUtil
{
    // 현재 UTC 시각을 초 단위(double)로 반환
    public static double UtcSec()
        => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;

    public static float DeltaTime { get; private set; }
    public static long LastTick { get; private set; }

    public static void Update(int curTick)
    {
        if(LastTick == 0)
        {
            LastTick = curTick;
            DeltaTime = 0;
            return;
        }

        int deltaTick = curTick - (int)LastTick;

        if (deltaTick < 0)
            deltaTick += int.MaxValue;

        DeltaTime = deltaTick / 1000f;
        LastTick = curTick;
    }
}
