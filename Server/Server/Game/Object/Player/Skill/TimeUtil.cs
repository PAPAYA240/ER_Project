using System;
using System.Collections.Generic;
using System.Text;

public static class TimeUtil
{
    // 현재 UTC 시각을 초 단위(double)로 반환
    public static double UtcSec()
        => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
}
