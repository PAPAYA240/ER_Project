using Google.Protobuf.Protocol;
using System.Threading;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

public class TheodoreController : MyPlayerController
{
    Material passiveMaterial, originMaterial;
    Renderer myRenderer;

    Transform handL;
    protected override void Init()
    {
        base.Init();

        myRenderer = this.GetComponentInChildren<Renderer>();
        if (myRenderer == null)
            return;
        originMaterial = myRenderer.material;
        passiveMaterial = Resources.Load<Material>("materials/effect/TheoPassiveMaterial");

        GameObject weapon = Managers.Resource.Instantiate($"Creature/Weapon/WP_Theodore_SP01_Sniperrifle_LOD");
        if (weapon != null)
        {
            handL = this.FindInDescendants(transform, "Equip_R");
            if (handL != null)
            {
                weapon.transform.SetParent(handL);
                weapon.transform.localPosition = Vector3.zero;
                weapon.transform.localRotation = Quaternion.identity;
                weapon.transform.localScale = Vector3.one;
            }
            else
                Debug.LogError("Weapon_L 트랜스폼을 찾을 수 없습니다.");
        }
    }
    //float speed = 10f;
    //void Update()
    //{
    //    if (bThrow)
    //    {
    //        weapons[1].transform.Translate(forwardDirection * speed * Time.deltaTime, Space.World);

    //        float distance = Vector3.Distance(weapons[1].transform.position, _startPosition);
    //        if (distance >= 10f)
    //        {
    //            bThrow = false;
    //            weapons[1].SetActive(false);
    //        }
    //    }
    //}
    //bool bThrow = false;
    //Vector3 forwardDirection;
    //Vector3 _startPosition;
    //private void Throw()
    //{
    //    GameObject weapon = weapons[1];
    //    weapon.SetActive(true);
    //    bThrow = true;
    //    weapon.transform.position = this.transform.position;
    //    forwardDirection = this.transform.forward;
    //    _startPosition = this.transform.position;
    //}

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
    }
}

