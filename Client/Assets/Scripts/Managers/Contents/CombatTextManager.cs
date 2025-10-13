using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CombatTextManager
{
    // 데미지, 회복, 보호막 등의 텍스트를 띄움
    // 오브젝트 풀링으로 사용할 예정. 한 50개 정도면 충분하지 않을까.

    public enum TextType { AdDamage, ApDamage, TrueDamage, HpRecovery, StaminaRecovery, Barrier, End }

    GameObject _canvas = null;

    public void Init()
    {
        Managers.Pool.CreatePool(Managers.Resource.Load<GameObject>($"Prefabs/UI/SubItem/DamageText"), 50);
    }

    public void SetCombatText(TextType type, float value, Vector3 worldPos)
    {
        GameObject go = Managers.Resource.Instantiate("UI/SubItem/DamageText");

        if(_canvas == null)
            _canvas = GameObject.Find("CombatTextCanvas");

        go.transform.SetParent(_canvas.transform);

        if (go != null)
        {
            go.GetComponent<UI_DamageText>().SetDamageText(type, value, worldPos);
        }
    }

}
