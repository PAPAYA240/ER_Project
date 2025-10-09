using Google.Protobuf.Protocol;
using System.Collections;
using UnityEngine;

public class CreatureController : BaseController
{
    #region CC기 면역
    protected UI_TargetingMark targetingMark = null;
    private bool _hasCrowdControl = false;
    public bool HasCrowdControl 
    {
        get
        {
            return _hasCrowdControl;
        }
        set
        {
            _hasCrowdControl = value;

            // 구속이 가능한 상태 시에 UI 띄우기
            if (_hasCrowdControl)
                targetingMark.ShowCCMark(this.gameObject);

            else
                targetingMark.HideCCMark();
        }
    }

    public void ApplyStun(float duration)
    {
        StartCoroutine(CoEndStun(duration));
    }

    IEnumerator CoEndStun(float duration)
    {
        Debug.Log($"Stun applied for {duration} seconds.");
        yield return new WaitForSeconds(duration);
        C_Stun stunPacket = new C_Stun()
        {
            ObjectId = ObjInfo.ObjectId,
            IsStun = false,
        };
        Managers.Network.Send(stunPacket);
    }
    #endregion

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

        // TODO : 예비 UI
        GameObject mark = Managers.Resource.Instantiate($"UI/Character/Theodore/MarkE");
        targetingMark = mark.gameObject.AddComponent<UI_TargetingMark>();
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

    public virtual void UseSkill(int skillId) {}

    public virtual void UseSkill(S_Skill skillPacket) {}

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

        CreatureController cc = targetObject.GetComponent<CreatureController>();
        if (cc == null) 
            return false;

        // 나 자신일 때
        if(cc.Id == Id) 
            return false;

        // 같은 팀일 때
        if (cc.ObjectType == Define.Object.OtherPlayer && cc.ObjInfo.Player.Team == ObjInfo.Player.Team)
            return false;

        // 대상이 죽었을 때 || 무적 상태일 때 || 시야 밖일 때(부시) 등등
        if (cc.State == CreatureState.Dead)
            return false;

        return true;
    }
}
