using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Text;

public class RestRegenEffect : IRegenEffect
{
    public bool IsActive => true;

    float bonusRegen = 200.0f;   // TEMP : 이건 고정인가

    public void OnTick(Player owner)
    {
        if (owner.State != CreatureState.Rest)
            return;

        float hpRegen = owner.Stat.HpRegen * bonusRegen;
        float staminaRegen = owner.Stat.StaminaRegen * bonusRegen;

        owner.Hp = MathF.Min(owner.Stat.MaxHp, owner.Hp + hpRegen);
        owner.Stamina = MathF.Min(owner.Stat.MaxStamina, owner.Stamina + staminaRegen);
    }
}

