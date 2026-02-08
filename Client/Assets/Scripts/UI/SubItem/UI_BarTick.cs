using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_BarTick : Monobehaviour
{
    enum Images
    {
        FillImage
    }

    float _value = 2000.0f;
    float _maxValue = 2000.0f;

    RectTransform _barRectTransform;

    GameObject _tickPrefab; // 눈금 프리팹
    public int TickInterval { get; set; } = 1000; // 눈금 간격 (예: 100 HP마다)

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
        _barRectTransform = GetImage((int)Images.FillImage).rectTransform;

        UpdateFillAmount();
        GenerateTick();
    }
    public void SetValue(float value)
    {
        _value = Mathf.Min(value, _maxValue);
        UpdateFillAmount();
    }

    public float GetValue()
    {
        return _value;
    }

    public void SetMaxValue(float maxValue)
    {
        _maxValue = maxValue;
        GenerateTick();
        UpdateFillAmount();
    }

    void UpdateFillAmount()
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

    private void GenerateTick()
    {
        foreach (GameObject tick in _ticks)
        {
            Destroy(tick);
        }
        _ticks.Clear();

        float barWidth = _barRectTransform.rect.width;
        float barHeight = _barRectTransform.rect.height;

        //딱 떨어지면 마지막 눈금을 표시 안하기 위해 -1
        int numTicks = (int)((_maxValue - 1) / TickInterval); 

        for(int i = 1; i <= numTicks; ++i)
        {
            float tickValue = i * TickInterval;

            float ratio = (float)tickValue / _maxValue;

            float x = -(barWidth * 0.5f) + (barWidth * ratio);

            GameObject newTick = Instantiate(_tickPrefab, _barRectTransform); 
            RectTransform tickRect = newTick.GetComponent<RectTransform>();

            // 눈금의 피벗과 앵커를 설정하여 위치를 정확하게 잡습니다.
            tickRect.anchorMin = new Vector2(0, 0.5f); // 좌측 중앙
            tickRect.anchorMax = new Vector2(0, 0.5f); // 좌측 중앙
            tickRect.pivot = new Vector2(0, 0.5f); // 자체 피벗을 좌측 중앙으로
            tickRect.localPosition = new Vector3(x, 0, 0); // Y는 체력바의 중앙, Z는 0

            _ticks.Add(newTick);
        }
    }
}
