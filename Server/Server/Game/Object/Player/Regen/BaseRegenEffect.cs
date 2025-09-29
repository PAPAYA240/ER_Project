using Server.Game;
using System;
using System.Collections.Generic;
using System.Text;

public class BaseRegenEffect : IRegenEffect
{
    public bool IsActive => true;

    public void OnTick(Player owner)
    {
        float hpRegen = owner.Stat.HpRegen;
        float staminaRegen = owner.Stat.StaminaRegen;

        owner.Hp = MathF.Min(owner.Stat.MaxHp, owner.Hp + hpRegen);
        owner.Stamina = MathF.Min(owner.Stat.MaxStamina, owner.Stamina +  staminaRegen);
    }
}

