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
    private static readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private static long _lastTicks;

    public static void Update()
    {
        long currentTicks = _stopwatch.ElapsedTicks;
        long deltaTicks = currentTicks - _lastTicks;
        DeltaTime = (float)deltaTicks / Stopwatch.Frequency;
        _lastTicks = currentTicks;
    }
}
