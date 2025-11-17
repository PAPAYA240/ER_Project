using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

public sealed class TimeUtil
{
    private static readonly Lazy<TimeUtil> _instance = new Lazy<TimeUtil>(() => new TimeUtil());
    public static TimeUtil Instance => _instance.Value;

    // atomic long, int 비트 변환으로 float DeltaTime 처리
    private long _lastTick;
    private int _deltaBits;
    private TimeUtil() { }

    // 현재 UTC 시각을 초 단위(double)로 반환
    public static double UtcSec()
        => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;

    public float DeltaTime => BitConverter.Int32BitsToSingle(Interlocked.CompareExchange(ref _deltaBits, 0, 0));
    public long LastTick => Interlocked.Read(ref _lastTick);

    public void Update(long curTick)
    {
        long last = Interlocked.Read(ref _lastTick);

        if (last == 0)
        {
            Interlocked.Exchange(ref _lastTick, curTick);
            Interlocked.Exchange(ref _deltaBits, BitConverter.SingleToInt32Bits(0f));
            return;
        }

        long deltaMs = unchecked(curTick - last);
        Interlocked.Exchange(ref _deltaBits, BitConverter.SingleToInt32Bits(deltaMs / 1000f));
        Interlocked.Exchange(ref _lastTick, curTick);
    }

    // 남은 시간(초) 계산
    public float RemainingSec(long endTick)
    {
        long last = Interlocked.Read(ref _lastTick); // atomic 읽기
        long deltaMs = unchecked(endTick - last);   // 래핑 안전
        return deltaMs / 1000f;
    }

    // now ≥ target 비교(래핑 안전)
    public bool IsPastOrNow(long target)
    {
        long now = Interlocked.Read(ref _lastTick); // atomic 읽기
        return unchecked(now - target) >= 0;
    }
}
