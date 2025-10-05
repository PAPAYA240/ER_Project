using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

public class TheodoreController : MyPlayerController
{
    // 머터리얼
    Material passiveMaterial, originMaterial;
    Renderer myRenderer;

    // 장비
    private GameObject _sniperRifle = null;

    // 이펙트
    List<GameObject> _currentEffectList = new List<GameObject>();

    protected override void Init()
    {
        if (!Add_Material())
        {
            Debug.LogError("TheodoreController : 필요한 Material이 할당되지 않았음");
            return;
        }

        if (!Equip_Weapon())
        {
            Debug.LogError("TheodoreController : 필요한 무기가 할당되지 않았음");
            return;
        }

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
  
    IEnumerator CoPassive()
    {
        Renderer sniperRender = _sniperRifle.GetComponentInChildren<Renderer>();
        myRenderer.material = passiveMaterial;
        sniperRender.material = passiveMaterial;
        Managers.FX.PlayEffect(Find_EffectList(KeyCode.W), this.transform);

        yield return new WaitForSeconds(4f);

        sniperRender.material = originMaterial;
        myRenderer.material = originMaterial;
    }

    public override void OnAttackTiming()
    {
        // 평타
        if (State == CreatureState.Attack)
        {
            Transform childTransform = Util.FindChildByName(_sniperRifle.transform, "ShotPoint");
            List<GameObject> EffectList = Managers.FX.PlayEffect(Find_EffectList(KeyCode.Z), childTransform);
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

    protected void Passive()
    {
        // 바라보는 방향대로 스크린 호출
        StartCoroutine(CoPassive());
        SendFXPacket(_keyCode);
        State = CreatureState.Idle;
    }

    #region Q Skill
    protected override void Skill_Q() 
    {
        ReadySkillQ();
        StartCoroutine(ShotSkillQ());
    }
    private void ReadySkillQ()
    {
        _sniperRifle.GetComponent<WeaponController>().PlayAnimation("SKILL_READY_Q", 0.1f);
        PlayAnimation("SKILL_START", 0.1f);

        _currentEffectList = Managers.FX.PlayEffect(Find_EffectList(KeyCode.Q), this.transform);
        _effectDuration =DataManager.PlayerFxDict[CharacterType.Theodore][KeyCode.Q][0].duration - 2.5f;

        indicator.EnableIndicator(KeyCode.Q);
        SendFXPacket(_keyCode);
    }

    private float _elapsedTime = 0f;
    private float _effectDuration = 0f;
    IEnumerator ShotSkillQ()
    {
        while (Input.GetKey(KeyCode.Q))
        {
            _elapsedTime += Time.deltaTime;
            _ratioSkillDuration = _elapsedTime / _effectDuration;

            if (_elapsedTime >= _effectDuration)
            {
                State = CreatureState.Idle;

                indicator.DisableAllIndicators();

                _ratioSkillDuration = 0f;
                _elapsedTime = 0;
                _effectDuration = 0;
                yield break;
            }
            yield return null;
        }

        indicator.DisableAllIndicators();
        _isUseSkill = true;
        _elapsedTime = 0;
        _effectDuration = 0;

        _sniperRifle.GetComponent<WeaponController>().PlayAnimation("SKILL_Q", 0.1f);

        foreach (GameObject effect in _currentEffectList)
        {
            if (effect != null)
            {
                Managers.FX.StopAndReturnEffect(effect);
                effect.SetActive(false);
            }
        }
        _currentEffectList.Clear();
        
        PlayAnimation("SKILL_Q", 0.1f);

        LookAtMouse();
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
        while (Input.GetKey(KeyCode.W) && !Input.GetMouseButtonDown(0))
        {
            // 오른 마우스 누르면 취소
            if(Input.GetMouseButtonDown(1))
            {
                indicator.DisableAllIndicators();
                yield break;
            }
            yield return null;
        }

        _isUseSkill = true;

        // w키를 떼거나, 왼 마우스를 누르거나
        // _currentEffectList = Managers.FX.PlayEffect(Find_EffectList(KeyCode.W), this.transform);
        indicator.DisableAllIndicators();
        
        LookAtMouse();

        State = CreatureState.Idle;

         yield break;
    }
    #endregion

    #region E Skill

    protected override void Skill_E()
    {
        indicator.EnableIndicator(KeyCode.E);
        StartCoroutine(ShotSkillE());
    }

    IEnumerator ShotSkillE()
    {
        while (Input.GetKey(KeyCode.E))
            yield return null;

        indicator.DisableAllIndicators();
        PlayAnimation("SKILL_E", 0.1f);

        LookAtMouse();
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
            yield return null;

        indicator.DisableAllIndicators();
        PlayAnimation("SKILL_R", 0.1f);

        LookAtMouse();
    }
    #endregion

    public override void PlayEffectFromServer(EffectInfo fxInfo)
    {
        StartCoroutine(CoPassive());
    }

    UI_IndicatorTheodore indicator;
    #region init
    private bool Equip_Weapon()
    {
       Transform RTransform = Util.FindChildByName(transform, "Equip_R");
       _equipTransform = Util.FindChildByName(transform, "Equip_L");
        if (_equipTransform == null || RTransform == null)
            return false;

        // 스나이퍼
        _sniperRifle = Managers.Resource.Instantiate($"Creature/Weapon/WP_Theodore_SP01_Sniperrifle_LOD");
        if (_sniperRifle != null)
        {
            if (RTransform != null)
            {
                _sniperRifle.gameObject.AddComponent<WeaponController>();

                _sniperRifle.transform.SetParent(RTransform);
                _sniperRifle.transform.localPosition = Vector3.zero;
                _sniperRifle.transform.localRotation = Quaternion.identity;
                _sniperRifle.transform.localScale = Vector3.one;
            }
        }
        else
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
            }
        }
        else
            return false;

        indicator = gameObject.GetOrAddComponent<UI_IndicatorTheodore>();

        return true;
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

