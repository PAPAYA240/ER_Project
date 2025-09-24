using Server.Game;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using static Lucene.Net.Util.AttributeSource;

class StatRegenerator
{
    private readonly Player _owner;
    private Timer _timer;
    private readonly int _interval = 1000;  // 1초마다 실행

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

    private void OnTick()
    {
        if (!_owner.CanRegenerate())
            return;

        _owner.Hp = Math.Min(_owner.Stat.MaxHp, _owner.Hp + _owner.Stat.HpRegen);
        _owner.Stamina = Math.Min(_owner.Stat.MaxStamina, _owner.Stamina + _owner.Stat.MaxStamina);

        _owner._isUpdatedStat = true;
    }
}

