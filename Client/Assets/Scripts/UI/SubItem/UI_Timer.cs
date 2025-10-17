using Google.Protobuf.Protocol;
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
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }
    public void StartTimer(int phase, float clientLocalTargetRealtimeSinceStartupEnd)
    {
        _coTimer = StartCoroutine(CoTimer(phase, clientLocalTargetRealtimeSinceStartupEnd));
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

        GetText((int)Texts.TimerText).text = minute + " : " + second.ToString("D2");
    }

    private void SetPhase(int phase)
    {
        GetText((int)Texts.PhaseText).text = $"Phase {phase}";
    }

    IEnumerator CoTimer(int phase, float clientLocalTargetRealtimeSinceStartupEnd)
    {
        SetPhase(phase);

        while (Time.realtimeSinceStartup < clientLocalTargetRealtimeSinceStartupEnd)
        {
            float remainingDuration = clientLocalTargetRealtimeSinceStartupEnd - Time.realtimeSinceStartup;
            SetTimer(Mathf.Max(0, Mathf.CeilToInt(remainingDuration))); // 음수 방지 및 올림 처리
            yield return null; // 매 프레임 업데이트
        }

        SetTimer(0); // 타이머 종료 시 0으로 설정
        Debug.Log($"Phase {phase} Synced Timer Finished!");
    }

    public void SetTimer(int phase, float clientLocalTargetRealtimeSinceStartupEnd)
    {
        StopTimer();
        StartTimer(phase, clientLocalTargetRealtimeSinceStartupEnd);
    }
}
