using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class UI_PickSceneUI : UI_Scene
{

    enum GameObjects 
    {
        CharFullSize,
        PickScrollView,
        SelectedCharacterImage_0,
        SelectedCharacterImage_1,
        SelectedCharacterImage_2,
        SelectedCharacterImage_3,
        SelectedCharacterImage_4,
        SelectedCharacterImage_5,
        SelectedCharacterImage_6,
        SelectedCharacterImage_7,
    }

    public Action<string> OnClickedPickButton = null;

    public override void Init()
    {
        base.Init();

        Bind<GameObject>(typeof(GameObjects));

        GetObject((int)GameObjects.PickScrollView).GetComponent<UI_PickScrollView>().OnButtonClicked += ClickedCharPickButton;
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

    public void ClickedCharPickButton(string charName)
    {
        OnClickedPickButton?.Invoke(charName);
    }

    public void ChangePickImage(CharacterType charType, int idx)
    {
        GameObject go = GetObject((int)GameObjects.SelectedCharacterImage_0 + idx);
        if (go == null)
            return;

        if (charType == CharacterType.CharacterNone)
            return;

        go.GetComponent<UI_SelectedCharacterImage>().SetCharImage(charType.ToString());
    }
    public void ChangeNickname(string nickname, int idx)
    {
        GameObject go = GetObject((int)GameObjects.SelectedCharacterImage_0 + idx);
        if (go == null)
            return;

        go.GetComponent<UI_SelectedCharacterImage>().SetName(nickname);
    }
}
