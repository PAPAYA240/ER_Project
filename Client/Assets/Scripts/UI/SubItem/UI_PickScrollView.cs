using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_PickScrollView : UI_Base
{
    enum GameObjects
    { 
        Content 
    }

    List<GameObject> _buttonList = new List<GameObject>();


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
        AddCharButton("Hyunwoo");
    }

    void Update()
    {
        
    }

    public void AddCharButton(string charName)
    {
        GameObject go = Managers.Resource.Instantiate("UI/SubItem/CharPickButton");
        if(null != go)
        {
            go.GetComponent<UI_CharPickButton>().SetChar(charName);
            go.transform.SetParent(GetObject((int)GameObjects.Content).transform);
            go.GetComponent<RectTransform>().localScale = new Vector3(0.5f, 0.5f, 0.5f);
            _buttonList.Add(go);
        }
    }
}
