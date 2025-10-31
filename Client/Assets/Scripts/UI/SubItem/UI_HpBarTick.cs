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
        Barrier,
        DelayedHp
    }

    float _hp = 2000.0f;
    float _barrier = 0.0f;
    float _maxHp = 2000.0f;
    float _targetTotalFillAmount; // 잔상 바가 목표로 할 fillAmount 값

    public float _delayedLerpSpeed = 5f; // 잔상 바가 현재 체력을 따라가는 속도


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
        // 잔상 HP 바가 목표 fillAmount를 천천히 따라가도록 Lerp 적용
        Image delayedHpImage = GetImage((int)Images.DelayedHp);
        if (delayedHpImage != null && delayedHpImage.fillAmount > _targetTotalFillAmount)
        {
            // currentTotalRatio보다 크다면, 즉 현재 체력(HP+Barrier)이 줄어들 때만 천천히 줄어들게 합니다.
            delayedHpImage.fillAmount = Mathf.Lerp(delayedHpImage.fillAmount, _targetTotalFillAmount, Time.deltaTime * _delayedLerpSpeed);
        }
        // Debug.Log($"Delayed: {delayedHpImage.fillAmount}, Target: {_targetTotalFillAmount}"); // 디버깅용
    }

    public override void Init()
    {
        Bind<Image>(typeof(Images));

        _tickPrefab = Managers.Resource.Load<GameObject>("Prefabs/UI/SubItem/HpTick");
        _hpBarRectTransform = GetImage((int)Images.Hp).rectTransform;

        UpdateFillAmount();
        GenerateTick();
    }
    public void SetHp(float hp)
    {
        _hp = Mathf.Min(hp, _maxHp);
        UpdateFillAmount();
    }
    public float GetHp()
    {
        return _hp;
    }

    public void SetBarrier(float barrier)
    {
        _barrier = barrier;
        GenerateTick();
        UpdateFillAmount();
    }

    public void SetMaxHp(float maxHp)
    {
        _maxHp = maxHp;
        GenerateTick();
        UpdateFillAmount();
    }

    void UpdateFillAmount()
    {
        float currentTotalRatio; // 현재 Hp + Barrier가 전체 MaxHp 대비 어느 정도인지
        float hpRatio;
        float barrierRatio;

        // 현재 HP + Barrier의 총합 계산
        float totalCurrentValue = _hp + _barrier;

        if (totalCurrentValue >= _maxHp) // 풀피 이상일 때는 HP+Barrier를 100%로
        {
            hpRatio = _hp / totalCurrentValue;
            barrierRatio = _barrier / totalCurrentValue;
            currentTotalRatio = 1f;
        }
        else // 풀피가 아닐 때는 MaxHP 기준
        {
            hpRatio = _hp / _maxHp;
            barrierRatio = _barrier / _maxHp;
            currentTotalRatio = totalCurrentValue / _maxHp; // MaxHp 대비 실제 총 비율
        }

        // === 즉시 반응하는 HP 및 배리어 UI 업데이트 ===
        GetImage((int)Images.Hp).fillAmount = hpRatio;

        // 배리어 위치 = HP fill이 끝나는 지점
        // (RectTransform 기준으로 anchor 이동)
        RectTransform rt = GetImage((int)Images.Barrier).rectTransform;
        rt.anchorMin = new Vector2(hpRatio, 0f);
        rt.anchorMax = new Vector2(Mathf.Min(hpRatio + barrierRatio, 1f), 1f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // --- 잔상 HP 바의 목표 값 설정 (새로 추가) ---
        // 잔상 바는 HP와 배리어를 합한 총량을 나타내므로, currentTotalRatio를 목표로 합니다.
        // 하지만 잔상 바의 fillAmount가 현재 체력바(HP+Barrier)보다 높아질 때(회복)는 즉시 따라가야 자연스럽습니다.
        Image delayedHpImage = GetImage((int)Images.DelayedHp);
        if (delayedHpImage.fillAmount < currentTotalRatio)
        {
            delayedHpImage.fillAmount = currentTotalRatio; // 회복 시 잔상 바 즉시 채우기
        }
        _targetTotalFillAmount = currentTotalRatio; // 잔상 바의 목표 fillAmount 설정
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

        float maxValue = _maxHp + _barrier;

        //딱 떨어지면 마지막 눈금을 표시 안하기 위해 -1
        int numTicks = (int)((maxValue - 1) / TickInterval); 

        for(int i = 1; i <= numTicks; ++i)
        {
            float tickValue = i * TickInterval;

            float ratio = (float)tickValue / maxValue;

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
