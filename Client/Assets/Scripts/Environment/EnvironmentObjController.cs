using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvironmentObjController : BaseController
{
    private long _nextUseTime = 0;
    public EnvType _envType;
    void Update()
    {
        // 쿨타임에 따른 시각적 처리
        //if (_nextUseTime > Environment.TickCount)
        {
            // 예: 오브젝트를 회색으로 변경하거나 파티클 비활성화
        }
        //else
        {
            // 예: 원래 색으로 돌리거나 파티클 활성화
        }
    }

    public void SetNextUseTime(long time)
    {
        _nextUseTime = time;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // UI_PlayerHUD에 상호작용 버튼 표시 요청
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
        }
    }
}
