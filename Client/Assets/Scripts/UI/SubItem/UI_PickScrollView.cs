using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_PickScrollView : UI_Base
{
    enum GameObjects
    { 
        Content 
    }

    Dictionary<string, GameObject> _buttonList = new Dictionary<string, GameObject>();

    public Action<string> OnButtonClicked = null; 

    public override void Init()
    {
        Bind<GameObject>(typeof(GameObjects));


    }

    private void Awake()
    {
        Init();
    }

    void Start()
    {
        AddCharButton("Rozzi");
        AddCharButton("Abigail");
        AddCharButton("Yuki");
        AddCharButton("Theodore");
        //AddCharButton("Hyunwoo");
    }

    void Update()
    {
        
    }

    public void AddCharButton(string charName)
    {
        GameObject go = Managers.Resource.Instantiate("UI/SubItem/CharPickButton");
        if(null != go)
        {
            UI_CharPickButton ui = go.GetComponent<UI_CharPickButton>();
            if(ui != null)
            {
                ui.SetChar(charName);
                ui.OnClicked += OnClickedPickButton;
            }
            go.transform.SetParent(GetObject((int)GameObjects.Content).transform);
            go.GetComponent<RectTransform>().localScale = new Vector3(0.5f, 0.5f, 0.5f);
            _buttonList.Add(charName, go);
        }
    }

    public void OnClickedPickButton(string Name)
    {
        OnButtonClicked?.Invoke(Name);
    }
}
