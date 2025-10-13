using Google.Protobuf.Protocol;
using System.Collections;
using UnityEngine;
using static Data.SkillEffectList;

public class TheodoreController : MyPlayerController
{
    // Material
    Material passiveMaterial, originMaterial;
    Renderer myRenderer;

    protected override void Init()
    {
        if (!Add_Material())  return;

        if (!Equip_Weapon()) return;

        base.Init();

        _attackRange = 10;
    }

    protected override void UpdateSkillKeyInput()
    {
        if (IsKeyInput == false && Input.GetKeyDown(KeyCode.Q))
        {
            _isUseSkill = true;
            _keyCode = KeyCode.Q;
        }
        else if (IsKeyInput == false && Input.GetKeyDown(KeyCode.W))
        {
            _isUseSkill = true;
            _keyCode = KeyCode.W;
        }
        else if (IsKeyInput == false && Input.GetKeyDown(KeyCode.E))
        {
            _isUseSkill = true;
            _keyCode = KeyCode.E;
        }
        else if (IsKeyInput == false && Input.GetKeyDown(KeyCode.R))
        {
            _isUseSkill = true;
            _keyCode = KeyCode.R;
        }
        else if (Input.GetKeyDown(KeyCode.F))
        {
            _isUseSkill = true;
            _keyCode = KeyCode.F;
        }
    }

    #region CC
    IEnumerator CoPassive()
    {
        Renderer sniperRender = _eqipWeapon.GetComponentInChildren<Renderer>();
        myRenderer.material = passiveMaterial;
        sniperRender.material = passiveMaterial;
        PlayEffectTransform(CreatureState.Skill, KeyCode.W);

        yield return new WaitForSeconds(4f);

        sniperRender.material = originMaterial;
        myRenderer.material = originMaterial;
    }
    protected void Passive()
    {
        // 바라보는 방향대로 스크린 호출
        StartCoroutine(CoPassive());
        SendFXPacket(_keyCode);
        State = CreatureState.Idle;
    }
    #endregion

    private void Bondage(GameObject target)
    {
        CreatureController myTarget = target.GetComponent<CreatureController>();
        float duration = DataManager.SkillDict[ObjInfo.Player.CharType][_keyCode].levels[myTarget.Stat.Level].effects[0].duration;

        C_Stun stunPacket = new C_Stun()
        {
            ObjectId = myTarget.ObjInfo.ObjectId,
            IsStun = true,
            Duration = duration,
        };
        Managers.Network.Send(stunPacket);

        target.GetComponent<CreatureController>().HasCrowdControl = false;

        PlayEffectTransform(CreatureState.Skill, KeyCode.Q, EffectType.HitTarget, _currentTarget);
        SendFXPacket(_keyCode);
    }

    #region Q Skill
    private float _elapsedTime = 0f;
    private float _effectDuration = 0f;
    private GameObject _currentTarget = null;

    protected override void Skill_Q() 
    {
        ReadySkillQ();

        StartCoroutine(ShotSkillQ());
    }

    private void ReadySkillQ()
    {
        PlayAnimation("SKILL_START", 0.1f);
        _eqipWeapon.GetComponent<WeaponController>().PlayAnimation("SKILL_READY_Q", 0.1f);

        PlayEffectTransform(CreatureState.Skill, _keyCode);
        SendFXPacket(_keyCode);

        _effectDuration = 5.0f; // TODO : 예시로 하드 코딩, 나중에 CC Data에서 가져와 줄 것
        indicator.EnableIndicator(KeyCode.Q);
    }
    
    IEnumerator ShotSkillQ()
    {
        // 1. 키를 누르고 있거나, 최대 충전 시간이 지나지 않은 경우
        while (Input.GetKey(KeyCode.Q) && _elapsedTime < _effectDuration)
        {
            if (Input.GetMouseButtonDown(1))
            {
                CancelSkill();
                yield break;
            }

            UpdateChargeState();
            yield return null;
        }

        // 2. 최대 충전 시간 초과 -> 스킬 취소
        if (_elapsedTime >= _effectDuration)
        {
            State = CreatureState.Idle;
            FinalizeSkillQ(_currentTarget, true);
            yield break;
        }

        FinalizeSkillQ(_currentTarget, false);
    }

    private void UpdateChargeState()
    {
        _elapsedTime += Time.deltaTime;
        _ratioSkillDuration = _elapsedTime / _effectDuration; 
        _currentTarget = TryGetAttackableObject();
    }

    bool bScreenInvo = false;
    private void FinalizeSkillQ(GameObject target, bool isMaxCharge)
    {
        indicator.DisableAllIndicators();
        _elapsedTime = 0;
        _effectDuration = 0;

        if (isMaxCharge)
        {
            _ratioSkillDuration = 0f;
            _isUseSkill = false;
        }
        else
        {
            _isUseSkill = true;
            
            // 애니메이션
            LookAtMouse();
            PlayAnimation("SKILL_Q", 0.1f);
            _eqipWeapon.GetComponent<WeaponController>().PlayAnimation("SKILL_Q", 0.1f);

            // 스턴 가능 경우 속박
            if (_currentTarget != null && _currentTarget.GetComponent<CreatureController>().HasCrowdControl)
            {
                Bondage(_currentTarget);
            }

            PlayEffect("FX_SkillFire");
        }
    }
    #endregion

    #region W Skill
    protected override void Skill_W()
    {
        State = CreatureState.Idle;
        indicator.EnableIndicator(KeyCode.W);
        StartCoroutine(ShotSkillW());
    }
  
    IEnumerator ShotSkillW()
    {
        // 1. 대기 : W 키를 누르고 있거나, 왼 마우스를 누르지 않았을 때
        while (Input.GetKey(KeyCode.W) && !Input.GetMouseButtonDown(0))
        {
            if(Input.GetMouseButtonDown(1))
            {
                CancelSkill();
                yield break;
            }
            yield return null;
        }

        bScreenInvo = true;
        FinalizeSkillW();
        yield break;
    }

    private void FinalizeSkillW()
    {
        /*
        일반 공격이 스크린을 통과하면 추가 스킬 피해를 주고 적 주변으로 확산된다.

       에너지 포(Q)가 스크린을 통과하면 잠시 후 증폭 스크린 전방으로 에너지를 발사하여 적에게 피해를 주고 
       아군을 회복시킨다. 발사한 에너지포의 방향과는 상관없이 무조건 중앙에 발사된다.

       스파크탄(E)이 스크린에 통과하면 평타처럼 적 주변으로 확산하고 투사체 속도와 사거리가 증가한다.
       */
        // 1. 상태 및 플래그 설정
        _isUseSkill = true;
        indicator.DisableAllIndicators();
        State = CreatureState.Idle;

        // 2. 마우스
        Vector3 mousePos = GetMouseWorldPosition();
        LookAtMouse();

        // 3. 이펙트 
        float playerYaw = transform.rotation.eulerAngles.y;
        Quaternion yawRotationOnly = Quaternion.Euler(0, playerYaw, 0);
        Quaternion desiredXRotation = Quaternion.Euler(-90f, 180f, 0);
        Quaternion finalRotation = yawRotationOnly * desiredXRotation;

        if (mousePos != Vector3.zero)
        {
           PlayEffectAtPosition(CreatureState.Skill, KeyCode.W, mousePos, finalRotation, EffectType.HitTarget);
            SendFXPacket(_keyCode);
        }
    }
    private void CancelSkill()
    {
        indicator.DisableAllIndicators();
        State = CreatureState.Idle;
    }

    #endregion

    #region E Skill
    protected override void Skill_E()
    {
        ReadySkillE();
    }

    private void ReadySkillE()
    {
        indicator.EnableIndicator(KeyCode.E);

        _currentTarget = TryGetAttackableObject();

        StartCoroutine(ShotSkillE());
    }
  
    IEnumerator ShotSkillE()
    {
        while (Input.GetKey(KeyCode.E))
        {
            if (Input.GetMouseButtonDown(1))
            {
                CancelSkill();
                yield break;
            }
            _currentTarget = TryGetAttackableObject();
            yield return null; 
        }

        CreatureController targetCreature = _currentTarget?.GetComponentInChildren<CreatureController>();

        _isUseSkill = true;
        indicator.DisableAllIndicators();
        PlayAnimation("SKILL_E", 0.1f);

        // 스피드 감소, 시야 제공, 공격 시 => [스킬 피해 추가, 속박]
        // 30/60/90/120/150(+스킬 증폭의 65%)
        if (targetCreature != null)
            StartCoroutine(AbilitySkillE(targetCreature));

        // 구속 가능
        LookAtMouse();
    }

    private IEnumerator AbilitySkillE(CreatureController creature)
    {
        float targetOrigionSpeed = creature.Speed;

        string decreaseSpeed = 
            DataManager.SkillDict[ObjInfo.Player.CharType][_keyCode].
            descriptionInfo["speed"][Stat.Level];

        int numberInt = int.Parse(decreaseSpeed);
        creature.Speed = creature.Speed * (1.0f - 0.01f * numberInt);

        // 이동 속도를 2동안 감소시킨다.
        yield return new WaitForSeconds(2.0f);
        creature.Speed = targetOrigionSpeed;
    }
    #endregion

    #region R Skill
    protected override void Skill_R()
    {
        State = CreatureState.Idle;

        indicator.EnableIndicator(KeyCode.R);

        StartCoroutine(ShootSkillR());
    }
    IEnumerator ShootSkillR()
    {
        while (Input.GetKey(KeyCode.R))
        {
            if (Input.GetMouseButtonDown(1))
            {
                CancelSkill();
                yield break;
            }
            yield return null; 
        }

        indicator.DisableAllIndicators();
        PlayAnimation("SKILL_R", 0.1f);
        
        // 속박 가능 시 => 속박
        if (_currentTarget != null && _currentTarget.GetComponent<CreatureController>().HasCrowdControl)
            Bondage(_currentTarget);

        LookAtMouse();
    }
    #endregion

    #region Util
    // 추가 데미지
    public float CalculateBonusDamage(float currentAttackPower)
    {
        string damagePercentageStr = DataManager.SkillDict[ObjInfo.Player.CharType][_keyCode].descriptionInfo["damage"][Stat.Level];
        if (!int.TryParse(damagePercentageStr, out int percentage))
            return 0f;

        float damageRatio = (float)percentage / 100f;

        float bonusDamage = currentAttackPower * damageRatio;

        return bonusDamage;
    }

    public override void OnAttackTiming()
    {
        // 평타
        if (State == CreatureState.Attack)
        {
            GameObject childTransform = Util.FindChildByName(_eqipWeapon.transform, "ShotPoint");
            PlayEffectTransform(CreatureState.Skill, KeyCode.Z, EffectType.Caster, null, childTransform.transform);
        }
        // 스킬
        else if (State == CreatureState.Skill)
        {
            switch (_keyCode)
            {
                case KeyCode.E:
                    SpawnProjectile();
                    return;
            }
        }
    }
    private Vector3 GetMouseWorldPosition()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) return Vector3.zero;

        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Map")))
            return hit.point;

        return Vector3.zero;
    }

    public override void PlayEffectFromServer(EffectInfo fxInfo)
    {
        PlayEffectTransform(CreatureState.Skill, (KeyCode)fxInfo.KeyCode);
        //StartCoroutine(CoPassive());
    }
    #endregion

    #region init
    UI_IndicatorTheodore indicator;
    private bool Equip_Weapon()
    {
       _equipTransform = Util.FindChildByName(transform, "Equip_L").transform;
        if (_equipTransform == null)
            return false;

        // 스파크 탄
        _projectile = Managers.Resource.Instantiate($"Creature/Weapon/WP_Theodore_Skill03_LOD");
        if (_projectile != null)
        {
            if (_equipTransform != null)
            {
                _projectile.transform.localPosition = _equipTransform.localPosition;
                _projectile.transform.localRotation = Quaternion.identity;
                _projectile.transform.localScale = Vector3.one;

                Projectile sparkProjectile = _projectile.AddComponent<Projectile>();
                sparkProjectile.Owner = this.gameObject;
            }
        }
        else
            return false;

        // Skill Indicator UI
        indicator = gameObject.GetOrAddComponent<UI_IndicatorTheodore>();

        return true;
    }
    public override void OnSkillConfirmed(S_Skill skillPacket)
    {
        base.OnSkillConfirmed(skillPacket);
    }

    private bool Add_Material()
    {
        myRenderer = this.GetComponentInChildren<Renderer>();
        if (myRenderer == null) return false;

        originMaterial = myRenderer.material;
        passiveMaterial = Resources.Load<Material>("materials/effect/TheoPassiveMaterial");
        if (passiveMaterial == null) return false;
        
        return true;
    }
    #endregion
}

