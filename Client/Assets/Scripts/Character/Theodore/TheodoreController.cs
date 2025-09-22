using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        // 마우스 바라보기
    void LookAtMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector3 direction = (hit.point - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = targetRotation;
        }
    }

    #region Q Skill
    protected override void Skill_Q()
    {
        ReadySkillQ();
        StartCoroutine(ShootSkillQ());
    }
    private void ReadySkillQ()
    {
        _currentEffectList = Managers.FX.PlayEffect(Find_EffectList(KeyCode.Q), this.transform);
        SendFXPacket(_keyCode);
    }

    IEnumerator ShootSkillQ()
    {
        while (Input.GetKey(KeyCode.Q))
            yield return null;

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

    protected override void Skill_W()
    {
        // 바라보는 방향대로 스크린 호출
        StartCoroutine(CoPassive());
        SendFXPacket(_keyCode);
    }

    #region E Skill

    protected override void Skill_E()
    {
        StartCoroutine(ShootSkillE());
    }

    IEnumerator ShootSkillE()
    {
        while (Input.GetKey(KeyCode.E))
            yield return null;

        PlayAnimation("SKILL_E", 0.1f);
      
        LookAtMouse();
    }
    public override void OnAttackTiming()
    {
        switch (_keyCode)
        {
            case KeyCode.E:
                SpawnProjectile();
                break;
        }
    }
    #endregion

    #region R Skill
    protected override void Skill_R()
    {
        StartCoroutine(ShootSkillR());
    }
    IEnumerator ShootSkillR()
    {
        while (Input.GetKey(KeyCode.R))
            yield return null;

        PlayAnimation("SKILL_R", 0.1f);

        LookAtMouse();
    }
    #endregion

    public override void PlayEffectFromServer(EffectInfo fxInfo)
    {
        StartCoroutine(CoPassive());
    }

    #region init
    private bool Equip_Weapon()
    {
       Transform RTransform = this.FindInDescendants(transform, "Equip_R");
       _equipTransform = this.FindInDescendants(transform, "Equip_L");
        if (_equipTransform == null || RTransform == null)
            return false;

        // 스나이퍼
        _sniperRifle = Managers.Resource.Instantiate($"Creature/Weapon/WP_Theodore_SP01_Sniperrifle_LOD");
        if (_sniperRifle != null)
        {
            if (RTransform != null)
            {
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

