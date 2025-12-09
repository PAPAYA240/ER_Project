using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_LevelUpButton : UI_Base
{
    enum Buttons
    { 
        LevelUpButton 
    }

    public Image _ImageComponent;
    public Sprite _basicImg;
    public Sprite _disabledImg;
    public Sprite _mouseOverImg;

    public override void Init()
    {
        Bind<Button>(typeof(Buttons));

        _ImageComponent = gameObject.GetComponent<Image>();
        if (null == _ImageComponent)
        {
            //Debug.Log("null : _ImageComponent");
        }

        //TODO 주소가 하드 코딩되어있음.
        _basicImg = Managers.Resource.Load<Sprite>("Sprite/Btn_LevelUp_Basic_02");
        _disabledImg = Managers.Resource.Load<Sprite>("Sprite/Btn_LevelUp_Disabled_02");
        _mouseOverImg = Managers.Resource.Load<Sprite>("Sprite/Btn_LevelUp_MouseOver_02");

        BindEvent(gameObject, OnPointerEnter, Define.UIEvent.PointerEnter);
        BindEvent(gameObject, OnPointerExit, Define.UIEvent.PointerExit);
    }

    void Update()
    {
        
    }

    void OnPointerEnter(PointerEventData data)
    {
        if (_ImageComponent != null)
        {
            _ImageComponent.sprite = _mouseOverImg;
        }
    }

    void OnPointerExit(PointerEventData data)
    {
        if (_ImageComponent != null)
        {
            _ImageComponent.sprite = _basicImg;
        }
    }
}
