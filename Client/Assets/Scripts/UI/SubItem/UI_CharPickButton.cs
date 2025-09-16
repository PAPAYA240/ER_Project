using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_CharPickButton : UI_Base
{
    enum Texts
    {
        Text
    }

    enum Buttons
    {
        Button
    }

    enum GameObjects
    {
        Frame,
        FrameOver,
        ButtonBg,
        ButtonBgOver
    }

    public string Name = "";
    public Action<string> OnClicked = null; 

    public override void Init()
    {
        Bind<TextMeshProUGUI>(typeof(Texts));
        Bind<Button>(typeof(Buttons));
        Bind<GameObject>(typeof(GameObjects));

        MouseOverEvent(false);

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

    public void SetChar(string charName)
    {
        Name = charName;
        GetButton((int)Buttons.Button).image.sprite = Managers.Resource.Load<Sprite>($"Sprite/CharResult_{charName}_S000");

        switch (charName)
        {
            case "Rozzi":
                GetText((int)Texts.Text).text = "����";
                break;
            case "Yuki":
                GetText((int)Texts.Text).text = "��Ű";
                break;
            case "Hyunwoo":
                GetText((int)Texts.Text).text = "����";
                break;
            case "Abigail":
                GetText((int)Texts.Text).text = "�ƺ����";
                break;
            case "Theodore":
                GetText((int)Texts.Text).text = "�׿�����";
                break;
        }

    }

    void MouseOverEvent(bool isOver)
    {
        GetObject((int)GameObjects.FrameOver).gameObject.SetActive(isOver);
        GetObject((int)GameObjects.ButtonBgOver).gameObject.SetActive(isOver);
        GetObject((int)GameObjects.Frame).gameObject.SetActive(!isOver);
        GetObject((int)GameObjects.ButtonBg).gameObject.SetActive(!isOver);
    }


    void OnPointerEnter(PointerEventData data)
    {
        MouseOverEvent(true);
    }

    void OnPointerExit(PointerEventData data)
    {
        MouseOverEvent(false);
    }

    public void OnClickedEvent()
    {
        OnClicked?.Invoke(Name);
    }
}
