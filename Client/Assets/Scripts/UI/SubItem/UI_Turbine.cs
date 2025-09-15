using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Turbine : UI_Base
{
    enum Images
    {
        TurbineBg,
        TurbineGauge,
        TurbineImage
    }

    enum Texts
    {
        Timer
    }

    public enum TurbineState { Ally, Enemy, Neutral, Off }

    Coroutine _coTimer = null;

    int _scoreDelay = 40;

    UI_PlayerHUD _playerHUD;

    public override void Init()
    {
        Bind<Image>(typeof(Images));
        Bind<TextMeshProUGUI>(typeof(Texts));

        SetImages(TurbineState.Neutral);
        _playerHUD = GetComponentInParent<UI_PlayerHUD>();
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

    public void CaptureTurbine(bool isAlly)
    {
        MyPlayerController mpc = Managers.Object.MyPlayer;

        if (null == mpc)
            return;

        if(_coTimer != null)
        {
            StopCoroutine(_coTimer);
            _coTimer = null;
        }

        if(isAlly)
        {
            //우리팀 
            SetImages(TurbineState.Ally);
        }
        else
        {
            //적팀
            SetImages(TurbineState.Enemy);
        }

        _coTimer = StartCoroutine("CoTimer");
    }

    IEnumerator CoTimer()
    {
        float tick = 0.05f;
        float remainTime = _scoreDelay;
        SetTimer(remainTime);
        SetGauge(remainTime);

        while (true)
        {
            yield return new WaitForSeconds(tick);
            remainTime -= tick;
            SetTimer(remainTime);
            SetGauge(remainTime);

            if (remainTime <= 0)
            {
                //TODO 점수가 오른다.
                remainTime = _scoreDelay;
            }
        }

    }

    void SetTimer(float time)
    {
        GetText((int)Texts.Timer).text = time.ToString("F0");
    }

    void SetGauge(float value)
    {
        GetImage((int)Images.TurbineGauge).fillAmount = (_scoreDelay - value) / _scoreDelay;
    }

    public void SetImages(TurbineState state)
    {
        if (_playerHUD == null)
            return;

        switch (state)
        {
            case TurbineState.Ally:
                GetImage((int)Images.TurbineBg).color = Color.blue;
                GetImage((int)Images.TurbineImage).sprite = _playerHUD.TurbineAlly;
                break;
            case TurbineState.Enemy:
                GetImage((int)Images.TurbineBg).color = Color.red;
                GetImage((int)Images.TurbineImage).sprite = _playerHUD.TurbineEnemy;
                break;
            case TurbineState.Neutral:
                GetImage((int)Images.TurbineBg).color = Color.grey;
                GetImage((int)Images.TurbineImage).sprite = _playerHUD.TurbineNeutral;
                break;
            case TurbineState.Off:
                GetImage((int)Images.TurbineBg).enabled = false;
                GetImage((int)Images.TurbineImage).rectTransform.sizeDelta = new Vector2(28, 28);
                GetImage((int)Images.TurbineImage).sprite = _playerHUD.TurbineOff;
                break;

        }
    }
}
