using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_HpBar : UI_Base
{
    enum Images
    { 
        Hp, 
        Barrier 
    }

    enum Texts
    { Text }

    float _hp = 1000.0f;
    float _barrier = 0.0f;
    float _maxHp = 1000.0f;

    private void Awake()
    {
        Init();
    }

    private void Start()
    {

    }

    private void Update()
    {
        
    }

    public override void Init()
    {
        Bind<Image>(typeof(Images));
        Bind<TextMeshProUGUI>(typeof(Texts));

        UpdateFillAmountAndText();
    }

    public void SetHp(float hp)
    {
        _hp = hp;
        UpdateFillAmountAndText();
    }
    public float GetHp()
    {
        return _hp;
    }

    public void SetBarrier(float barrier)
    {
        _barrier = barrier;
        UpdateFillAmountAndText();
    }

    public void SetMaxHp(float maxHp)
    {
        _maxHp = maxHp;
        UpdateFillAmountAndText();
    }

    void UpdateFillAmountAndText()
    {
        float hpRatio;
        float barrierRatio;

        if (_hp + _barrier >= _maxHp) // 풀피 이상일 때는 HP+Barrier를 100%로
        {
            float total = _hp + _barrier;
            hpRatio = _hp / total;
            barrierRatio = _barrier / total;
        }
        else // 풀피가 아닐 때는 MaxHP 기준
        {
            hpRatio = _hp / _maxHp;
            barrierRatio = _barrier / _maxHp;
        }

        // 체력 Fill
        GetImage((int)Images.Hp).fillAmount = hpRatio;

        // 배리어 위치 = HP fill이 끝나는 지점
        // (RectTransform 기준으로 anchor 이동)
        RectTransform rt = GetImage((int)Images.Barrier).rectTransform;
        rt.anchorMin = new Vector2(hpRatio, 0f);
        rt.anchorMax = new Vector2(Mathf.Min(hpRatio + barrierRatio, 1f), 1f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        if (_barrier > 0f)
            GetText((int)Texts.Text).text = $"{_hp.ToString("F0")}(+{_barrier.ToString("F0")}) / {_maxHp.ToString("F0")}";
        else
            GetText((int)Texts.Text).text = $"{_hp.ToString("F0")} / {_maxHp.ToString("F0")}";
    }

    public void PlusHp(float value)
    {
        SetHp(Mathf.Min(_hp + value, _maxHp));
    }
    public void PlusBarrier(float value)
    {
        _barrier += value;
    }
    public void MinusHp(float value)
    {
        //흡수할 수치 계산
        float absorbed = Mathf.Min(_barrier, value);

        _barrier -= absorbed;

        float remaining = value - absorbed;

        SetHp(Mathf.Max(_hp - remaining, 0));
    }

}
