using Server.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using static Lucene.Net.Util.AttributeSource;

class StatRegenerator
{
    private readonly Player _owner;

    // 누적 시간(ms)과 틱 간격
    private float _elapsedMs;
    private int _intervalMs;

    // 동작 on/off
    private bool _enabled;

    private readonly List<IRegenEffect> _effects = new List<IRegenEffect>();

    public StatRegenerator(Player owner, int intervalMs = 1000)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _intervalMs = Math.Max(1, intervalMs);
    }

    public void Update()
    {
        if (!_enabled)
            return;

        _elapsedMs += TimeUtil.DeltaTime * 1000f;

        // interval을 초과한 만큼 틱을 여러 번 처리(로딩/일시정지 후 catch-up)
        while (_elapsedMs >= _intervalMs)
        {
            _elapsedMs = 0;

            if (!_owner.CanRegenerate())
                continue;

            // 스냅샷 순회 → 효과 중간 제거 안전
            foreach (var effect in _effects.ToList())
            {
                effect.OnTick(_owner);
                if (!effect.IsActive)
                    _effects.Remove(effect);
            }

            // 상태 변경됨 → 송신은 Player.CheckUpdateStat가 담당
            _owner._isUpdatedStat = true;
        }
    }

    public void Start()
    {
        _enabled = true;
        _elapsedMs = 0;
    }

    public void Stop()
    {
        _enabled = false;
        _elapsedMs = 0;
    }

    public void AddEffect(IRegenEffect effect)
    {
        if (effect == null)
            return;
        _effects.Add(effect);
    }

    public void RemoveEffect(IRegenEffect effect)
    {
        if (effect == null)
            return;
        _effects.Remove(effect);
    }

    public void ClearEffects() => _effects.Clear();

    public void SetIntervalMs(int intervalMs)
    {
        _intervalMs = Math.Max(1, intervalMs);
    }
}

