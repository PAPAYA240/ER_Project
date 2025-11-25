using System.Collections;
using System.Collections.Generic;
using Google.Protobuf.Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_ReadyButton : UI_Base
{
    enum State
    {
        Notyet,
        Ready,
        None
    }

    enum Texts { Text }

    Sprite _basicSprite;
    Sprite _disabledSprite;
    Sprite _pressedSprite;
    Sprite _rolloverSprite;

    Image _image;

    State _state = State.Notyet;

    public override void Init()
    {
        Bind<TextMeshProUGUI>(typeof(Texts));

        _basicSprite = Managers.Resource.Load<Sprite>("Sprite/Btn_MatchingStart_01");
        _disabledSprite = Managers.Resource.Load<Sprite>("Sprite/Btn_MatchingStart_Disabled_01");
        _pressedSprite = Managers.Resource.Load<Sprite>("Sprite/Btn_MatchingStart_Pressed_01");
        _rolloverSprite = Managers.Resource.Load<Sprite>("Sprite/Btn_MatchingStart_Rollover_01");

        _image = GetComponent<Image>();
        _image.sprite = _basicSprite;

        SetText("준비 중");

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

    void Update()
    {
        
    }

    void SetText(string newString)
    {
        GetText((int)Texts.Text).text = newString;
    }

    void OnClicked(PointerEventData eventData)
    {
        _image.sprite = _pressedSprite;
        SetText("준비 완료");

        //TODO
        C_ReadyBtn readyBtnPkt = new C_ReadyBtn();
        Managers.Network.Send(readyBtnPkt);
    }

    void OnPointerEnter(PointerEventData eventData)
    {
        if (_state == State.Notyet)
        {
            _image.sprite = _rolloverSprite;
            SetText("준비 완료");
        }
    }

    void OnPointerExit(PointerEventData eventData)
    {
        if (_state == State.Notyet)
        {
            _image.sprite = _basicSprite;
            SetText("준비 중");
        }
        else if (_state == State.Ready)
        {
            _image.sprite = _disabledSprite;
            SetText("준비 완료");
        }
    }

    public void OnReady()
    {
        _state = State.Ready;
        _image.sprite = _disabledSprite;
        SetText("준비 완료");
    }
}
