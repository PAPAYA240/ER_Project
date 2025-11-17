using Google.Protobuf.Protocol;
using UnityEngine;

public class CreatureController : BaseController
{
    public override StatInfo Stat
    {
        get { return base.Stat; }
        set { base.Stat = value; UpdateMaxHp(); UpdateHp(); UpdateMaxStamina(); UpdateStamina();  }
    }

    public override float Hp
    {
        get { return Stat.Hp; }
        set { base.Hp = Mathf.Clamp(value, 0, Stat.MaxHp); UpdateHp(); }
    }

    public override float MaxHp
    {
        get { return Stat.MaxHp; }
        set { base.MaxHp = value; UpdateMaxHp(); }
    }

    public override float Barrier
    {
        get { return Stat.Barrier; }
        set { base.Barrier = value; UpdateBarrier(); }
    }

    public override float Stamina
    {
        get { return Stat.Stamina; }
        set { base.Stamina = Mathf.Clamp(value, 0, Stat.MaxStamina); UpdateStamina(); }
    }

    public override float MaxStamina
    {
        get { return Stat.MaxStamina; }
        set { base.MaxStamina = value; UpdateMaxStamina(); }
    }

    public virtual float HpRegen
    {
        get { return Stat.HpRegen; }
        set { Stat.HpRegen = Mathf.Max(value, 0); }
    }

    public virtual float StaminaRegen
    {
        get { return Stat.StaminaRegen; }
        set { Stat.StaminaRegen = Mathf.Max(value, 0); }
    }

    public virtual float Attack
    {
        get { return Stat.Attack; }
        set { Stat.Attack = Mathf.Max(value, 0); }
    }

    public virtual float FixedDefensePenetration
    {
        get { return 0f; }
    }

    public virtual float PercentageDefensePenetration
    {
        get { return 0f; }
    }

    public virtual float Defense
    {
        get { return Stat.Defense; }
        set { Stat.Defense = Mathf.Max(value, 0); }
    }

    public virtual float AttackRange
    {
        get { return Stat.AttackRange; }
        set { Stat.AttackRange = Mathf.Max(value, 0); }
    }

    public virtual bool Untargetable { get; set; } = false; // 대상 지정불가 상태
    public virtual bool Unstoppable { get; set; } = false; // 이동 방해 면역

    virtual protected void UpdateHp()
    {

    }

    virtual protected void UpdateMaxHp()
    {

    }

    virtual protected void UpdateBarrier()
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
        SyncPos();
        base.Init();
    }
    public virtual void OnDamaged()
    {
    }

    public virtual void OnDead()
    {
        State = CreatureState.Dead;

        //GameObject effect = Managers.Resource.Instantiate("Effect/DieEffect");
        //effect.transform.position = transform.position;
        //effect.GetComponent<Animator>().Play("START");
        //GameObject.Destroy(effect, 0.5f);
    }

    public void Snare(S_Snare packet, CharacterType charType)
    {
        // Destory Proejctile
         
        Managers.FX.PlayStatusEffect(this.gameObject, charType, packet.Duration);
    }

    public virtual void UseSkill(int skillId) {}

    public virtual void UseSkill(S_Skill skillPacket) {}

    public virtual void OnHitboxCollision(KeyCode kc, KeyCode tkc) 
    {
    }
    public void ChangeStat(StatInfo growth)
    {
        Stat.Attack += growth.Attack;
        Stat.Defense += growth.Defense;
        MaxHp += growth.MaxHp;
        Stat.HpRegen += growth.HpRegen;
        Stamina += growth.MaxStamina;
        MaxStamina += growth.MaxStamina;
        Stat.StaminaRegen += growth.StaminaRegen;
        Hp += growth.MaxHp;
    }

    public bool IsAttackable(GameObject targetObject)
    {
        if (targetObject == null)
            return false;

        CreatureController cc = targetObject.GetComponentInChildren<CreatureController>();
        if (cc == null) 
            return false;

        if (cc.Untargetable)
            return false;

        // 나 자신일 때
        if(cc.Id == Id) 
            return false;

        // 같은 팀일 때
        if (cc.ObjInfo.Player?.Team == ObjInfo.Player?.Team)
            return false;

        // 대상이 죽었을 때 || 무적 상태일 때 || 시야 밖일 때(부시) 등등
        if (cc.State == CreatureState.Dead)
            return false;

        return true;
    }

    #region Shader
    // 벽 파란색 막기
    protected void UnActiveShaderXRay()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            foreach (Material mat in renderer.materials)
            {
                if (mat.HasProperty("_OccludedColor"))
                {
                    Color occludedColor = mat.GetColor("_OccludedColor");
                    occludedColor.a = 0f; 
                    mat.SetColor("_OccludedColor", occludedColor);
                }
            }
        }
    }
    #endregion
}
