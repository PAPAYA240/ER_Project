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
    public static int LastTick { get; private set; }

    public static void Update(int curTick)
    {
        if(LastTick == 0)
        {
            LastTick = curTick;
            DeltaTime = 0f;
            return;
        }

        uint deltaMs = (uint)curTick - (uint)LastTick;
        DeltaTime = deltaMs / 1000f;
        LastTick = curTick;
    }

    // 남은 시간(초) 계산
    public static float RemainingSec(int endTick)
    {
        uint deltaMs = (uint)endTick - (uint)LastTick; // 음수면 큰 양수로 자동 래핑
        return deltaMs / 1000f;
    }

    // now ≥ target 비교(래핑 안전)
    public static bool IsPastOrNow(int now, int target)
        => unchecked(now - target) >= 0;
}
