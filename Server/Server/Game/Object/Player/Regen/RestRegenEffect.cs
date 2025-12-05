using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Text;
using static IRegenEffect;

public class RestRegenEffect : IRegenEffect
{
    public bool IsActive => true;
    public StatRegenType Effect => StatRegenType.RestRegen;

    float bonusRegen = 200.0f;   // TEMP : 이건 고정인가

    public void OnTick(Player owner)
    {
        if (owner.State != CreatureState.Rest)
            return;

        float hpRegen = owner.HpRegen * bonusRegen;
        float staminaRegen = owner.StaminaRegen * bonusRegen;

        //owner.Hp = MathF.Min(owner.MaxHp, owner.Hp + hpRegen);
        owner.ApplyHeal(hpRegen);
        owner.Stamina = MathF.Min(owner.MaxStamina, owner.Stamina + staminaRegen);
    }
}

