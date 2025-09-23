using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UI_SkillPopupLevelUpValue : UI_Base
{
    enum Texts { Key, Values }

    public override void Init()
    {
        Bind<TextMeshProUGUI>(typeof(Texts));

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

    public void SetKeyText(string str)
    {
        GetText((int)Texts.Key).text = str;
    }

    public void SetValuesText(string str)
    {
        GetText((int)Texts.Values).text = str;
    }
}
