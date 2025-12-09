using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class FX_Follower : MonoBehaviour
{
    public Transform target;            // 따라갈 플레이어
    public float rightOffset = 0f;      // 화면 기준 오른쪽으로 얼마나
    public float upOffset = 2.0f;       // 위로 얼마나
    public float forwardOffset = 0f;    // 필요하면 살짝 앞/뒤

    private Camera _cam;

    private void Start()
    {
        _cam = Camera.main;
    }

    private void LateUpdate()
    {
        if (target == null || _cam == null)
            return;

        // 카메라 기준 방향 벡터
        Vector3 camRight = _cam.transform.right;
        Vector3 camForward = _cam.transform.forward;

        Vector3 worldPos =
            target.position +
            camRight * rightOffset +
            Vector3.up * upOffset +
            camForward * forwardOffset;

        transform.position = worldPos;
    }

    public void SetTarget(Transform t)
    {
        target = t;
    }
}