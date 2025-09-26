using Server.Game;
using System;
using System.Collections.Generic;
using System.Text;

public class HealPackEffect : IRegenEffect
{
    private float _amount;
    private float _perTick;
    private float _remaining;

    public  HealPackEffect(float amount, float durationSeconds)
    {
        _amount = amount;
        _perTick = amount / durationSeconds;
        _remaining = amount;
    }

    public bool IsActive => _remaining > 0;

    public void OnTick(Player owner)
    {
        if(_remaining <= 0) 
            return;

        float heal = MathF.Min(_perTick, _remaining);
        owner.Hp = MathF.Min(owner.Stat.MaxHp, owner.Hp + heal);    
        _remaining -= heal;
    }
}

