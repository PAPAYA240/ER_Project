using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_HpBarTick : UI_Base
{
    enum Images
    {
        Hp,
        Barrier
    }

    float _hp = 2000.0f;
    float _barrier = 0.0f;
    float _maxHp = 2000.0f;

    RectTransform _hpBarRectTransform;

    GameObject _tickPrefab; // 눈금 프리팹
    public int TickInterval { get; set; } = 100; // 눈금 간격 (예: 100 HP마다)
    //private Color tickColor = Color.gray; // 눈금 색상

    List<GameObject> _ticks = new List<GameObject>();

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

    public override void Init()
    {
        Bind<Image>(typeof(Images));

        _tickPrefab = Managers.Resource.Load<GameObject>("Prefabs/UI/SubItem/HpTick");
        _hpBarRectTransform = GetImage((int)Images.Hp).rectTransform;

        UpdateFillAmountAndText();
        GenerateTick();
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
        GenerateTick();
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

    private void GenerateTick()
    {
        foreach (GameObject tick in _ticks)
        {
            Destroy(tick);
        }
        _ticks.Clear();

        float barWidth = _hpBarRectTransform.rect.width;
        float barHeight = _hpBarRectTransform.rect.height;

        //딱 떨어지면 마지막 눈금을 표시 안하기 위해 -1
        int numTicks = (int)((_maxHp - 1) / TickInterval); 

        for(int i = 1; i <= numTicks; ++i)
        {
            float tickValue = i * TickInterval;

            float ratio = (float)tickValue / _maxHp;

            float x = -(barWidth * 0.5f) + (barWidth * ratio);

            GameObject newTick = Instantiate(_tickPrefab, _hpBarRectTransform); 
            RectTransform tickRect = newTick.GetComponent<RectTransform>();

            // 눈금의 피벗과 앵커를 설정하여 위치를 정확하게 잡습니다.
            tickRect.anchorMin = new Vector2(0, 0.5f); // 좌측 중앙
            tickRect.anchorMax = new Vector2(0, 0.5f); // 좌측 중앙
            tickRect.pivot = new Vector2(0, 0.5f); // 자체 피벗을 좌측 중앙으로

            if (i % 10 == 0)
            {
                // 로컬 포지션으로 설정 (healthBarRect의 좌측 가장자리를 0으로 가정)
                tickRect.localPosition = new Vector3(x, 0, 0); // Y는 체력바의 중앙, Z는 0
            }
            else
            {
                tickRect.sizeDelta = new Vector2(1, barHeight * 0.5f);
                tickRect.localPosition = new Vector3(x, barHeight * 0.25f, 0); // Y는 체력바의 중앙, Z는 0
            }


            // 눈금 이미지의 색상 설정
            //Image tickImage = newTick.GetComponent<Image>();
            //if (tickImage != null)
            //{
            //    tickImage.color = tickColor;
            //}

            _ticks.Add(newTick);
        }
    }
}
