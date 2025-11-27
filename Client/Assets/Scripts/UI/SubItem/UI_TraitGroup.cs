using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_TraitGroup : UI_Base
{
    enum TraitGroupType { Havoc, Chaos, Fortification, Support, None }

    enum Images { Panel, Icon  }

    enum GameObjects { Button_0, Button_1, Button_2, Button_3 }

    private Material _material;

    private static UI_TraitGroup _currentSelected;

    [SerializeField]
    TraitGroupType _traitType;

    static Color _havocColor = new Color(1, 0.1875f, 0.1875f, 1);
    static Color _chaosColor = new Color(0.640625f, 0.1875f, 1, 1);
    static Color _fortificationColor = new Color(0.1875f, 0.5f, 1, 1);
    static Color _supportColor = new Color(0.1875f, 1, 0.5f, 1);

    public override void Init()
    {
        Bind<Image>(typeof(Images));
        Bind<GameObject>(typeof(GameObjects));

        _material = GetImage((int)Images.Panel).material;

        GetObject((int)GameObjects.Button_0).GetComponent<UI_TraitButton>().OnSelected += SetSelected;
        GetObject((int)GameObjects.Button_1).GetComponent<UI_TraitButton>().OnSelected += SetSelected;
        GetObject((int)GameObjects.Button_2).GetComponent<UI_TraitButton>().OnSelected += SetSelected;
        GetObject((int)GameObjects.Button_3).GetComponent<UI_TraitButton>().OnSelected += SetSelected;

        SetSelected(false);
    }

    private void Awake()
    {
        Init();
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    // 이 오브젝트를 선택/해제하는 함수 
    public void SetSelected(bool isSelected)
    {
        if (isSelected)
        {
            // 이전에 선택된 객체가 있다면, 그 객체를 흑백으로 돌림
            if (_currentSelected != null && _currentSelected != this)
            {
                _currentSelected.SetSelected(false); // 재귀 호출 방지를 위해 false
            }
            // 컬러
            ApplyBlendValue(0f);

            switch (_traitType)
            {
                case TraitGroupType.Havoc:
                    GetImage((int)Images.Icon).color = _havocColor;
                    break;
                case TraitGroupType.Chaos:
                    GetImage((int)Images.Icon).color = _chaosColor;
                    break;
                case TraitGroupType.Fortification:
                    GetImage((int)Images.Icon).color = _fortificationColor;
                    break;
                case TraitGroupType.Support:
                    GetImage((int)Images.Icon).color = _supportColor;
                    break;
            }
            
            _currentSelected = this;
        }
        else
        {
            // 선택 해제 시 흑백
            ApplyBlendValue(1f);
            GetImage((int)Images.Icon).color = new Color(0.3f, 0.3f, 0.3f, 1);
            if (_currentSelected == this)
            {
                _currentSelected = null;
            }
        }
    }

    //셰이더 프로퍼티 수정 함수
    private void ApplyBlendValue(float blendValue)
    {
        if (_material == null)
            return;

        _material.SetFloat("_IsNonColor", blendValue);
    }

    public void SelectTrait(int button)
    {
        if (Managers.Info.IsReady)
            return;

        GetObject((int)GameObjects.Button_0 + button).GetComponent<UI_TraitButton>().OnSelected.Invoke(true);
    }
}
