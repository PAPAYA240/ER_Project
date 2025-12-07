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

    private readonly float RestRegenRatio = 0.2f;

    public void OnTick(Player owner)
    {
        if (owner.State != CreatureState.Rest)
            return;

        float hpRegen = owner.MaxHp * RestRegenRatio;
        float staminaRegen = owner.MaxStamina * RestRegenRatio;

        owner.ApplyHeal(hpRegen);
        owner.Stamina = MathF.Min(owner.MaxStamina, owner.Stamina + staminaRegen);
    }
}

