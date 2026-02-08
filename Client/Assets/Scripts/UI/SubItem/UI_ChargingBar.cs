using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_ChargingBar : Monobehaviour
{
    enum Images { ChargingBarImage }

    enum Texts { SkillName, ChargingBarTimer }

    private float _fullChargingTime; // Time when the charging ratio becomes 1
    private float _maxChargingTime; // Maximum amount of time that charging can be maintained

    private Coroutine _coTimer = null;

    public override void Init()
    {
        Bind<Image>(typeof(Images));
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

    public void SetChargingBar(string skillName, float fullChargingTime, float maxChargingTime)
    {
        GetText((int)Texts.SkillName).text = skillName;
        _fullChargingTime = fullChargingTime;
        _maxChargingTime = maxChargingTime;
        Stop();
        _coTimer = StartCoroutine(CoChargingBar());
    }

    IEnumerator CoChargingBar()
    {
        float curTime = 0;

        while (true)
        {
            curTime += Time.deltaTime; 
            GetImage((int)Images.ChargingBarImage).fillAmount = curTime / _fullChargingTime;
            GetText((int)Texts.ChargingBarTimer).text = curTime.ToString("F1");

            if (curTime > _maxChargingTime)
            {
                _coTimer = null;
                break;
            }

            yield return null;
        }
    }


    public void Stop()
    {
        if (_coTimer != null)
        {
            StopCoroutine(_coTimer);
            _coTimer = null;
        }
    }
}
