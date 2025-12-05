using Server.Game;
using System;
using System.Collections.Generic;
using System.Text;
using static IRegenEffect;

public class BaseRegenEffect : IRegenEffect
{
    public bool IsActive => true;
    public StatRegenType Effect => StatRegenType.BaseRegen;

    public void OnTick(Player owner)
    {
        float hpRegen = owner.HpRegen;
        float staminaRegen = owner.StaminaRegen;

        owner.ApplyHeal(hpRegen);
        owner.Stamina = MathF.Min(owner.MaxStamina, owner.Stamina + staminaRegen);
    }
}

