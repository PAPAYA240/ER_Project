using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UI_Credit : UI_Base
{
    enum Texts { ValueText }


    int _credit = 0;

    public override void Init()
    {
        Bind<TextMeshProUGUI>(typeof(Texts));

        UpdateText();
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

    void SetCredit(int credit)
    {
        _credit = credit;
        UpdateText();
    }

    void UpdateText()
    {
        GetText((int)Texts.ValueText).text = _credit.ToString();
    }

    public void PlusCredit(int credit)
    {
        SetCredit(_credit + credit);
    }

    public void MinusCredit(int credit)
    {
        SetCredit(Mathf.Max(0, _credit - credit));
    }
    public void UseCredit(int credit)
    {
        if (_credit < credit)
            return;

        MinusCredit(credit);
    }
}
