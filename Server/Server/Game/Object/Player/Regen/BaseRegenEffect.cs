using Server.Game;
using System;
using System.Collections.Generic;
using System.Text;

public class BaseRegenEffect : IRegenEffect
{
    public bool IsActive => true;

    public void OnTick(Player owner)
    {
        float hpRegen = owner.HpRegen;
        float staminaRegen = owner.StaminaRegen;

        owner.Hp = MathF.Min(owner.MaxHp, owner.Hp + hpRegen);
        owner.Stamina = MathF.Min(owner.MaxStamina, owner.Stamina + staminaRegen);
    }
}

