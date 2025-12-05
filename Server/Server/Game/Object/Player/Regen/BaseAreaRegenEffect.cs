using Server.Game;
using System;
using System.Collections.Generic;
using System.Text;
using static IRegenEffect;

public class BaseAreaRegenEffect : IRegenEffect
{
    public bool IsActive => true;
    public StatRegenType Effect => StatRegenType.BaseAreaRegen;

    private readonly float BaseAreaHpRegen = 500f;
    private readonly float BaseAreaStaminaRegen = 500f;


    public void OnTick(Player owner)
    {
        owner.Hp = MathF.Min(owner.MaxHp, owner.Hp + BaseAreaHpRegen);
        owner.Stamina = MathF.Min(owner.MaxStamina, owner.Stamina + BaseAreaStaminaRegen);
    }
}

