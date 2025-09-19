using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using UnityEngine;
using static Define;

public class CreatureController : BaseController
{
    Define.Object _object = Define.Object.Unknown;
    public Define.Object ObjectType
    {
        get { return _object; }
        set { _object = value; }
    }

    public override StatInfo Stat
    {
        get { return base.Stat; }
        set { base.Stat = value; UpdateMaxHp(); UpdateHp(); UpdateMaxStamina(); UpdateStamina();  }
    }

    public override float Hp
    {
        get { return Stat.Hp; }
        set { base.Hp = value; UpdateHp(); }
    }

    public override float MaxHp
    {
        get { return Stat.MaxHp; }
        set { base.MaxHp = value; UpdateMaxHp(); }
    }

    public override float Stamina
    {
        get { return Stat.Stamina; }
        set { base.Stamina = value; UpdateStamina(); }
    }

    public override float MaxStamina
    {
        get { return Stat.MaxStamina; }
        set { base.MaxStamina = value; UpdateMaxStamina(); }
    }

    virtual protected void UpdateHp()
    {

    }

    virtual protected void UpdateMaxHp()
    {

    }

    virtual protected void UpdateStamina()
    {

    }

    virtual protected void UpdateMaxStamina()
    {

    }

	protected override void Init()
    {
		base.Init();
    }

    public virtual void OnDamaged()
    {

    }

    public virtual void OnDead()
    {
        State = CreatureState.Dead;

        GameObject effect = Managers.Resource.Instantiate("Effect/DieEffect");
        effect.transform.position = transform.position;
        effect.GetComponent<Animator>().Play("START");
        GameObject.Destroy(effect, 0.5f);
    }

    public virtual void UseSkill(int skillId) {}

    public virtual void UseSkill(S_Skill skillPacket) {}

    public void ChangeStat(StatInfo growth)
    {
        Stat.Attack += growth.Attack;
        Stat.Defense += growth.Defense;
        Stat.MaxHp += growth.MaxHp;
        Stat.HpRegen += growth.HpRegen;
        Stat.Stamina += growth.MaxStamina;
        Stat.MaxStamina += growth.MaxStamina;
        Stat.StaminaRegen += growth.StaminaRegen;
        Hp = Stat.Hp + growth.MaxHp;
    }
}
