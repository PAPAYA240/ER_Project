using UnityEngine;
using UnityEngine.UI;

public class UI_WardLifeBar : Monobehaviour
{
    enum Images
    { FillImage }

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
        UpdateFillAmountAndText();
    }

    public void SetValue(float value)
    {
        _value = Mathf.Min(value, _maxValue);
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
    }

    public void PlusValue(float value)
    {
        SetValue(Mathf.Min(_value + value, _maxValue));
    }
    public void MinusValue(float value)
    {
        SetValue(Mathf.Max(_value - value, 0));
    }

    public void SetColor(Color color)
    {
        Image fillImage = GetImage((int)Images.FillImage);
        if (fillImage == null)
            return;
        fillImage.color = color;
    }
}
