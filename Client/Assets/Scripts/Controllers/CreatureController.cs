using Google.Protobuf.Protocol;
using UnityEngine;
using UnityEngine.AI;

public class CreatureController : BaseController
{
    [SerializeField] private LayerMask _monsterMask;
    [SerializeField] private LayerMask _playerMask;

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
        SetAttackableLayerMask();
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
         Managers.FX.PlayStatusEffect(this.gameObject, charType, 4.0f);
    }

    public virtual void UseSkill(int skillId) {}

    public virtual void UseSkill(S_Skill skillPacket) {}

    public virtual void OnHitboxCollision(KeyCode kc, KeyCode tkc) 
    {
    }
    public void ChangeStat(StatInfo stat)
    {
        Attack = stat.Attack;
        Defense = stat.Defense;
        MaxHp = stat.MaxHp;
        if(State != CreatureState.Dead)
            Hp += stat.Hp;
        Stat.HpRegen = stat.HpRegen;
        MaxStamina = stat.MaxStamina;
        Stamina += stat.Stamina;
        Stat.StaminaRegen = stat.StaminaRegen;
    }

    // lagacy code

    //public void ChangeStat(StatInfo growth)
    //{
    //    Stat.Attack += growth.Attack;
    //    Stat.Defense += growth.Defense;
    //    MaxHp = Stat.MaxHp + growth.MaxHp;
    //    Hp += growth.MaxHp;
    //    Stat.HpRegen += growth.HpRegen;
    //    MaxStamina = Stat.MaxStamina + growth.MaxStamina;
    //    Stamina += growth.MaxStamina;
    //    Stat.StaminaRegen += growth.StaminaRegen;
    //}
    public GameObject GetAttackableUnderCursor(int mask = default, float radius = 0.1f)
    {
        if (mask == default)
            mask = GetAttackableLayerMask();

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.SphereCast(ray, radius, out RaycastHit hit, 1000f, mask))
        {
            var cc = hit.collider.GetComponentInChildren<CreatureController>();
            if (cc != null && IsAttackable(cc, out _))
            {
                return cc.gameObject;
            }
        }

        return null;
    }

    public bool IsAttackable(GameObject targetObject, out InvalidTargetReason reason)
    {
        if (targetObject == null)
        {
            reason = InvalidTargetReason.InvalidNull;
            return false;
        }

        CreatureController cc = targetObject.GetComponentInChildren<CreatureController>();
        bool isAttackable = IsAttackable(cc, out var invalidReason);
        reason = invalidReason;
        return isAttackable;
    }

    public bool IsAttackable(CreatureController cc, out InvalidTargetReason reason)
    {
        if (cc == null)
        {
            reason = InvalidTargetReason.InvalidNull;
            return false;
        }

        // 나 자신일 때
        if (cc.Id == Id)
        {
            reason = InvalidTargetReason.InvalidSelf;
            return false;
        }

        // 같은 팀일 때
        if (cc.ObjInfo.Player != null && cc.ObjInfo.Player.Team == ObjInfo.Player.Team)
        {
            reason = InvalidTargetReason.InvalidAlly;
            return false;
        }

        // 지정 불가 상태일 때
        if (cc.Untargetable)
        {
            reason = InvalidTargetReason.InvalidUntargetable;
            return false;
        }

        // 대상이 죽었을 때 
        if (cc.State == CreatureState.Dead)
        {
            reason = InvalidTargetReason.InvalidDead;
            return false;
        }

        // 대상이 시야 밖일 때
        if (!IsVisibleObject(cc.Id))
        {
            reason = InvalidTargetReason.InvalidInvisible;
            return false;
        }

        // 대상이 은신 상태일 때
        if (IsHiding(cc))
        {
            reason = InvalidTargetReason.InvalidHiding;
            return false;
        }

        reason = InvalidTargetReason.InvalidValid;
        return true;
    }

    private bool IsVisibleObject(int id)
    {
        if (Managers.Scene.CurrentScene is GameScene scene)
            return scene.IsVisibleObject(id);

        return false;
    }

    private bool IsHiding(CreatureController cc)
    {
        if(cc is PlayerController pc)
        {
            if (pc.IsHide)
                return false;
            return true;
        }

        return false;
    }

    private int GetAttackableLayerMask()
    {
        if (_monsterMask == default || _playerMask == default)
            SetAttackableLayerMask();

        return _monsterMask | _playerMask;
    }

    private void SetAttackableLayerMask()
    {
        _monsterMask = 1 << LayerMask.NameToLayer("Monster");
        _playerMask = 1 << LayerMask.NameToLayer("Player");
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
