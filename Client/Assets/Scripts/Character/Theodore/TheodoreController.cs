using Data;
using Google.Protobuf.Protocol;
using System;
using System.Collections;
using UnityEngine;
using static Data.SkillEffectList;

public class TheodoreController : MyPlayerController
{
    private Material _passiveMaterial, _originMaterial;
    private UI_IndicatorTheodore _indicator;
    private GameObject _skillTarget;
    private Renderer _myRenderer;

    private float _effectDuration = 0f;
    private float _elapsedTime = 0f;

    protected override void Init()
    {
        if (!Add_Material())  return;
        if (!Equip_Weapon()) return;

        base.Init();
        _attackRange = 10;

    }

    #region Skill
    protected override void UpdateSkillKeyInput()
    {
        if (_isInputLocked) return;

        if (Input.GetKeyDown(KeyCode.Q)) PrepareSkill(KeyCode.Q);
        else if (Input.GetKeyDown(KeyCode.W)) PrepareSkill(KeyCode.W);
        else if (Input.GetKeyDown(KeyCode.E)) PrepareSkill(KeyCode.E);
        else if (Input.GetKeyDown(KeyCode.R)) PrepareSkill(KeyCode.R);
        else if (Input.GetKeyDown(KeyCode.F)) _keyCode = KeyCode.F;
    }

    private void PrepareSkill(KeyCode key)
    {
        if (!EnabledSkill(key)) return;
        _keyCode = key;

        // 차징 방식은 별도 처리
        if (key == KeyCode.Q)
        {
            State = CreatureState.Charging;
            StartCoroutine(ChargingSkill());
            return;
        }

        Action onConfirm = () => CallSkill(key);
        Action onCancel = () => CancelSkill();
        StartCoroutine(AimingSkillCo(key, onConfirm, onCancel));
    }

    private IEnumerator AimingSkillCo(KeyCode key, Action onConfirm, Action onCancel)
    {
        _isInputLocked = true;
        _indicator.EnableIndicator(key);

        while (!Input.GetKeyUp(key) && !Input.GetMouseButtonDown(0))
        {
            if (Input.GetMouseButtonDown(1))
            {
                onCancel?.Invoke();
                yield break;
            }
            yield return null;
        }
        onConfirm?.Invoke();
    }

    private void CallSkill(KeyCode key)
    {
        _isUseSkill = true;
        State = CreatureState.Skill;
        _indicator.DisableAllIndicators();

        LookAtMouse();

        switch (key)
        {
            case KeyCode.W:
                PlayEffectAtPosition(CreatureState.Skill, KeyCode.W, GetMouseWorldPosition(), GetIndicatorRotation(), EffectType.HitTarget);
                SendFXPacket(KeyCode.W);
                StartCoroutine(DelayAnimation());
                return;

            case KeyCode.E:
                PlayAnimation("SKILL_E", 0.1f);
                CreatureController targetCreature = _skillTarget?.GetComponentInChildren<CreatureController>();
                if (targetCreature != null)
                    StartCoroutine(ApplyDebuffE(targetCreature));
                break;

            case KeyCode.R:
                PlayAnimation("SKILL_R", 0.1f);
                break;
        }
    }
    #endregion
  
    #region Q Skill (Charging Type)
    IEnumerator ChargingSkill()
    {
        _indicator.EnableIndicator(KeyCode.Q);
        PlayAnimation("CHARGING", 0.1f);
        _eqipWeapon.GetComponent<WeaponController>().PlayAnimation("CHARGING", 0.1f);

        _elapsedTime = 0f;
        _effectDuration = 2.0f; // DataManager에서 가져오는 것을 권장

        while (Input.GetKey(KeyCode.Q) && _elapsedTime < _effectDuration)
        {
            _elapsedTime += Time.deltaTime;
            _ratioSkillDuration = _elapsedTime / _effectDuration;
            _skillTarget = TryGetAttackableObject();
            yield return null;
        }

        // 최대 시간 초과 시 스킬 사용 안함
        _isUseSkill = _elapsedTime < _effectDuration; 
        FinalizeSkillQ();
    }
    private void FinalizeSkillQ()
    {
        _indicator.DisableAllIndicators();
        _elapsedTime = 0;
        _agent.speed = _originSpeed;

        if (_isUseSkill)
        {
            State = CreatureState.Skill;
            _agent.ResetPath();
            LookAtMouse();
            PlayAnimation("SKILL_Q", 0.1f);
            _eqipWeapon.GetComponent<WeaponController>().PlayAnimation("SKILL_Q", 0.1f);

            PlayEffect("FX_SkillFire");
            return;
        }

        _ratioSkillDuration = 0f;
    }
    #endregion

    #region E Skill
    private IEnumerator ApplyDebuffE(CreatureController creature)
    {
        float originalSpeed = creature.Speed;

        yield return new WaitForSeconds(2.0f); // 지속시간도 DataManager에서...

        creature.Speed = originalSpeed;
    }
    #endregion

    #region CC기
    IEnumerator CoPassive()
    {
        Renderer sniperRender = _eqipWeapon.GetComponentInChildren<Renderer>();
        _myRenderer.material = _passiveMaterial;
        sniperRender.material = _passiveMaterial;
        PlayEffectTransform(CreatureState.Skill, KeyCode.W);

        yield return new WaitForSeconds(4f);

        sniperRender.material = _originMaterial;
        _myRenderer.material = _originMaterial;
    }
    protected void Passive()
    {
        // 바라보는 방향대로 스크린 호출
        StartCoroutine(CoPassive());
        SendFXPacket(_keyCode);
        State = CreatureState.Idle;
    }
    #endregion

    #region 충돌 시 조건 처리
    public override void OnHitboxCollision(KeyCode kc, KeyCode tkc)
    {
        base.OnHitboxCollision(kc, tkc);

        if (tkc == KeyCode.Q)
        {
        }
        else if (tkc == KeyCode.E)
        {
        }
    }
    
    public override void OnObjectCollision(GameObject target, KeyCode key)
    {
        base.OnObjectCollision(target, key);

        CreatureController cc = target.GetComponentInChildren<CreatureController>();
        if (cc == null) return;

        if (key == KeyCode.Q)
        {
            _skillTarget = target;
        }
        else if (key == KeyCode.E)
        {
            int level = 1; // TODO : 예비 레벨
            float duration = DataManager.SkillDict[ObjInfo.Player.CharType][KeyCode.E].levels[level].effects[0].duration;

            Managers.FX.PlayStatusEffect(target, CharacterType.Theodore, duration);
        }
    }

    #endregion

    #region Skill 취소
    private IEnumerator DelayAnimation()
    {
        State = CreatureState.Skill;

        yield return new WaitForSeconds(0.2f);

        State = CreatureState.Idle;
        IsKeyInput = false;
        StartCoroutine(InputLockCancel());
    }
    public override void OnSkillAnimationEnd()
    {
        StartCoroutine(InputLockCancel());
    }
    private void CancelSkill()
    {
        _indicator.DisableAllIndicators();

        _agent.ResetPath();

        State = CreatureState.Idle;

        StartCoroutine(InputLockCancel());
    }
    #endregion

    #region 보조 함수
    private IEnumerator InputLockCancel()
    {
        yield return new WaitForSeconds(0.3f);
        _isInputLocked = false;
    }
    private Quaternion GetIndicatorRotation()
    {
        float playerYaw = transform.rotation.eulerAngles.y;
        Quaternion yawRotationOnly = Quaternion.Euler(0, playerYaw, 0);
        Quaternion desiredXRotation = Quaternion.Euler(-90f, 180f, 0);
        return yawRotationOnly * desiredXRotation;
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
    public override void PlayEffectFromServer(EffectInfo fxInfo)
    {
        PlayEffectTransform(CreatureState.Skill, (KeyCode)fxInfo.KeyCode);
        //StartCoroutine(CoPassive());
    }
    #endregion

    #region init
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
        _indicator = gameObject.GetOrAddComponent<UI_IndicatorTheodore>();

        return true;
    }
    public override void OnSkillConfirmed(S_Skill skillPacket)
    {
        base.OnSkillConfirmed(skillPacket);
    }

    private bool Add_Material()
    {
        _myRenderer = this.GetComponentInChildren<Renderer>();
        if (_myRenderer == null) return false;

        _originMaterial = _myRenderer.material;
        _passiveMaterial = Resources.Load<Material>("materials/effect/TheoPassiveMaterial");
        if (_passiveMaterial == null) return false;
        
        return true;
    }
    #endregion
}

