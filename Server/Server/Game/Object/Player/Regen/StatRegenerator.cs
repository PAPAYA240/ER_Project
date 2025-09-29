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
    private Timer _timer;
    private readonly int _interval = 1000;  // 1초마다 실행

    private readonly List<IRegenEffect> _effects = new List<IRegenEffect>();

    public StatRegenerator(Player owner) {  _owner = owner; }

    public void Start()
    {
        _timer = new Timer(_ => OnTick(), null, 0, _interval);
    }

    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
    }

    public void AddEffect(IRegenEffect effect)
    {
        _effects.Add(effect);
    }

    private void OnTick()
    {
        if (!_owner.CanRegenerate())
            return;

        foreach(var effect in _effects.ToList())
        {
            effect.OnTick(_owner);

            if(!effect.IsActive)
                _effects.Remove(effect);
        }

        _owner._isUpdatedStat = true;
    }
}

