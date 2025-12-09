using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class BuffUIFollower : MonoBehaviour
{
    public Transform target;            // 따라갈 플레이어
    //public float rightOffset = 0.5f;    // 화면 기준 오른쪽으로 얼마나
    //public float upOffset = 1.0f;       // 위로 얼마나
    //public float forwardOffset = 0f;    // 필요하면 살짝 앞/뒤

    public float pixelRight = 50f; // 화면 픽셀 기준
    public float pixelUp = 30f;

    private Camera _cam;

    private void Awake()
    {
        _cam = Camera.main;
    }

    private void LateUpdate()
    {
        if (target == null || _cam == null)
            return;

        //// 위치: 월드 기준
        //Vector3 worldPos =
        //    target.position +
        //    Vector3.right * rightOffset +
        //    Vector3.up * upOffset;

        //// 카메라 기준 방향 벡터
        //Vector3 camRight = _cam.transform.right;
        //Vector3 camUp = _cam.transform.up;
        //Vector3 camForward = _cam.transform.forward;
        //
        //// 타겟 위치 + 카메라 기준 오프셋
        //Vector3 worldPos =
        //    target.position +
        //    camRight * rightOffset +
        //    camUp * upOffset +
        //    camForward * forwardOffset;

        // 1. 타겟 월드 위치 → 화면 좌표(픽셀)
        Vector3 screenPos = _cam.WorldToScreenPoint(target.position);

        // 2. 화면 기준으로 오른쪽/위로 픽셀만큼 이동
        screenPos.x += pixelRight;
        screenPos.y += pixelUp;

        // 3. 다시 월드 좌표로 복구 (z는 원래 값 유지해야 함)
        Vector3 worldPos = _cam.ScreenToWorldPoint(screenPos);


        transform.position = worldPos;
        transform.rotation = _cam.transform.rotation;
    }

    public void SetTarget(Transform t)
    {
        target = t;
    }
}