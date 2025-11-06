using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Collections.Generic;
using static Server.Data.DataUtils;

public class CooldownController_Tick : ICooldownController
{
    private readonly Player _owner;
    private readonly Func<int> _nowTick;

    private readonly Dictionary<KeyCode, CoolTime> _coolDownDict = new Dictionary<KeyCode, CoolTime>();
    class CoolTime
    {
        public int EndTick;         // 논리용 절대 종료 tick(ms)
        public bool Fired;          // 이번 사이클 Ready 이벤트 발사 여부(중복 방지)
    }

    public event Action<KeyCode> OnReady;

    public CooldownController_Tick(Player owner, Func<int> nowTickProvider = null)
    {
        _owner = owner;
        _nowTick = nowTickProvider ?? (() => (int)TimeUtil.LastTick);
    }

    public bool IsReady(KeyCode key)
    {
        if (!_coolDownDict.TryGetValue(key, out var e))
            return true; // 엔트리 없음 = 준비됨

        // now >= end 이면 준비됨 (래핑 안전)
        return TimeUtil.IsPastOrNow(TimeUtil.LastTick, e.EndTick);
    }

    public float GetRemaining(KeyCode key)
    {
        if (!_coolDownDict.TryGetValue(key, out var e))
            return 0f; // 없음 = 0초

        // 과거/현재(끝난 상태)이면 0 
        if (TimeUtil.IsPastOrNow(TimeUtil.LastTick, e.EndTick))
            return 0f;

        // 미래인 경우에만 남은 초 계산
        return TimeUtil.RemainingSec(e.EndTick);
    }

    // 쿨타임 시작. startTick=null이면 지금(LastTick)부터
    public void Start(KeyCode key, float durationSec, int? startTick = null)
    {
        int now = TimeUtil.LastTick;
        int start = startTick ?? now;
        if (TimeUtil.IsPastOrNow(now, start))
            start = now;

        int end = unchecked(start + SecToMs(durationSec));

        var e = GetOrCreate(key);
        e.EndTick = end;
        e.Fired = false;
    }

    // 외부에서 남은 시간 x초 감소
    public void Reduce(KeyCode key, float seconds)
    {
        if (seconds <= 0f)
            return;
        if (!_coolDownDict.TryGetValue(key, out var e))
            return;

        int delta = SecToMs(seconds);
        e.EndTick = unchecked(e.EndTick - delta);

        _owner.SendSkillCostPacket(key, GetRemaining(key));
    }

    // 남은 시간을 강제 설정(초). 0 이하면 즉시 Ready
    public void SetRemaining(KeyCode key, float seconds)
    {
        int now = TimeUtil.LastTick;

        var e = GetOrCreate(key);
        e.Fired = false;

        if (seconds <= 0f)
            e.EndTick = now; // 즉시 완료 판정
        else
            e.EndTick = unchecked(now + SecToMs(seconds));

        _owner.SendSkillCostPacket(key, GetRemaining(key));
    }

    //// 매 틱 호출: 이번 틱에 Ready가 된 스킬들을 찾아 OnReady 1회씩 호출
    //// 한 틱에 하나만 Ready 이벤트 발화
    //public void Update(int nowTick)
    //{
    //    KeyCode? fireKey = null;

    //    foreach (var (key, e) in _coolDownDict)      // 등록된 스킬들 순회
    //    {
    //        if (e.Fired)
    //            continue;         // 이미 Ready 쐈던 건 스킵

    //        if (TimeUtil.IsPastOrNow(nowTick, e.EndTick)) // now >= End
    //        {
    //            e.Fired = true;             // 중복 발사 방지 마킹
    //            fireKey = key;              // 이번 틱에 Ready 낼 대상 지정
    //            break;                      // (기본 구현) 한 번에 하나만 쏨
    //        }
    //    }

    //    if (fireKey.HasValue)
    //        OnReady?.Invoke(fireKey.Value); // 락 밖/루프 밖에서 콜백
    //}

    // --- Helpers ---
    private static int SecToMs(float sec) => (int)Math.Max(0, Math.Round(sec * 1000.0));

    private CoolTime GetOrCreate(KeyCode key)
    {
        if (!_coolDownDict.TryGetValue(key, out var e))
        {
            e = new CoolTime();
            _coolDownDict[key] = e;
        }
        return e;
    }
}

