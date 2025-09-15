using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;

public class UI_Timer : UI_Base
{
    enum Texts
    {
        PhaseText,
        TimerText
    }

    Coroutine _coTimer = null;

    public override void Init()
    {
        Bind<TextMeshProUGUI>(typeof(Texts));
    }
    private void Awake()
    {
        Init();

        StartTimer(1, 1200);
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }
    public void StartTimer(int phase, int seconds)
    {
        _coTimer = StartCoroutine(CoTimer(phase, seconds));
    }

    public void StopTimer()
    {
        if (null == _coTimer)
            return;

        StopCoroutine(_coTimer);
        _coTimer = null;
    }

    private void SetTimer(int seconds)
    {
        int minute = seconds / 60;
        int second = seconds % 60;

        GetText((int)Texts.TimerText).text = minute + " : " + second;
    }

    private void SetPhase(int phase)
    {
        GetText((int)Texts.PhaseText).text = $"Phase {phase}";
    }

    IEnumerator CoTimer(int phase,int seconds)
    {
        SetTimer(seconds);
        SetPhase(phase);

        int remain = seconds;

        while (true)
        {
            yield return new WaitForSeconds(1);
            remain--;
            SetTimer(remain);

            if (remain <= 0)
                break;
        }
    }
}
