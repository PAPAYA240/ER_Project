using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Bar : UI_Base
{
    enum Images
    { FillImage }

    enum Texts
    { Text }

    float _value = 1000.0f;
    float _maxValue = 1000.0f;

    private void Awake()
    {
        Init();
    }

    private void Start()
    {
        
    }

    public override void Init()
    {
        Bind<Image>(typeof(Images));
        Bind<TextMeshProUGUI>(typeof(Texts));

        UpdateFillAmountAndText();
    }

    public void SetValue(float value)
    {
        _value = value;
        UpdateFillAmountAndText();
    }
    public float GetValue()
    {
        return _value;
    }

    public void SetMaxValue(float maxValue)
    {
        _maxValue = maxValue;
        UpdateFillAmountAndText();
    }

    void UpdateFillAmountAndText()
    {
        GetImage((int)Images.FillImage).fillAmount = _value / _maxValue;
        GetText((int)Texts.Text).text = _value.ToString("F0") + " / " + _maxValue.ToString("F0");
    }

    public void PlusValue(float value)
    {
        SetValue(Mathf.Min(_value + value, _maxValue));
    }
    public void MinusValue(float value)
    {
        SetValue(Mathf.Max(_value - value, 0));
    }

}
