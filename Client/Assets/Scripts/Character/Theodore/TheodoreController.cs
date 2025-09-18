using Google.Protobuf.Protocol;
using System.Collections;
using UnityEngine;

public class TheodoreController : MyPlayerController
{
    // 정보

    // 머터리얼
    Material passiveMaterial, originMaterial;
    Renderer myRenderer;

    // 장비
    private GameObject _sniperRifle = null;
    private GameObject _sparkBullet = null;
    private Transform _equipLTransform = null;

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
        else if (Input.GetKeyDown(KeyCode.D))
        {

        }
        else if (Input.GetKeyDown(KeyCode.F))
        {
            _isUseSkill = true;
            _keyCode = KeyCode.F;
        }
    }
 
    IEnumerator CoPassive()
    {
        myRenderer.material = passiveMaterial;
        Managers.FX.PlayEffect(Find_EffectList(KeyCode.W), this.transform);

        yield return new WaitForSeconds(4f);
        myRenderer.material = originMaterial;
    }
    protected override void PassiveSkill()
    {
        //StartCoroutine(CoPassive());
    }


    #region E Skill
    private Vector3 _lastForward;
   
    protected override void Skill_E()
    {
        PlayAnimation("SKILL_E", 0.1f);

        _sparkBullet.SetActive(true);
        _lastForward = this.transform.forward;
    }

    IEnumerator CoSkillE()
    {
        float elapsedTime = 0f;
        float duration = 3.0f;
        float speed = 10f;

        Vector3 startPosition = _sparkBullet.transform.position;
        while (elapsedTime < duration)
        {
            _sparkBullet.transform.position += _lastForward * speed * Time.deltaTime;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        _sparkBullet.SetActive(false);
    }

    public override void OnAttackTiming()
    {
        switch (_keyCode)
        {
            case KeyCode.E:
                _sparkBullet.transform.position = _equipLTransform.position;
                StartCoroutine(CoSkillE());
                break;
        }
    }

    #endregion

    #region Skill
    protected override void Skill_Q()
    {
        PlayAnimation("SKILL_Q", 0.1f);
        Managers.FX.PlayEffect(Find_EffectList(KeyCode.Q), this.transform);
        SendFXPacket(_keyCode);
    }

    protected override void Skill_W()
    {
        // 바라보는 방향대로 스크린 호출
        _isUseSkill = false;
        PlayAnimation("WAIT", 0.1f);
        StartCoroutine(CoPassive());
        SendFXPacket(_keyCode);
    }

    protected override void Skill_R()
    {
        PlayAnimation("SKILL_R", 0.1f);
    }
    #endregion

    #region init
    private bool Equip_Weapon()
    {
       Transform RTransform = this.FindInDescendants(transform, "Equip_R");
       _equipLTransform = this.FindInDescendants(transform, "Equip_L");
        if (_equipLTransform == null || RTransform == null)
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
        _sparkBullet = Managers.Resource.Instantiate($"Creature/Weapon/WP_Theodore_Skill03_LOD");
        if (_sparkBullet != null)
        {
            if (_equipLTransform != null)
            {
                _sparkBullet.transform.localPosition = _equipLTransform.localPosition;
                _sparkBullet.transform.localRotation = Quaternion.identity;
                _sparkBullet.transform.localScale = Vector3.one;
                _sparkBullet.SetActive(false);
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

