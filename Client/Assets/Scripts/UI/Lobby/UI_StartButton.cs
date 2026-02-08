using System.Collections;
using System.Collections.Generic;
using Google.Protobuf.Protocol;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_StartButton : Monobehaviour
{
    Sprite _basicSprite;
    Sprite _rolloverSprite;

    Image _image;

    public override void Init()
    {
        _basicSprite = Managers.Resource.Load<Sprite>("Sprite/Btn_MatchingStart_01");
        _rolloverSprite = Managers.Resource.Load<Sprite>("Sprite/Btn_MatchingStart_Rollover_01");

        _image = GetComponent<Image>();
        _image.sprite = _basicSprite;

        BindEvent(gameObject, OnClicked, Define.UIEvent.Click);
        BindEvent(gameObject, OnPointerEnter, Define.UIEvent.PointerEnter);
        BindEvent(gameObject, OnPointerExit, Define.UIEvent.PointerExit);
    }

    private void Awake()
    {
        Init();
    }

    void Start()
    {
        
    }

    void OnClicked(PointerEventData eventData)
    {
        C_StartBtn startBtnPkt = new C_StartBtn();
        Managers.Network.Send(startBtnPkt);
    }

    void OnPointerEnter(PointerEventData eventData)
    {
        _image.sprite = _rolloverSprite;
    }

    void OnPointerExit(PointerEventData eventData)
    {
        _image.sprite = _basicSprite;
    }
}

