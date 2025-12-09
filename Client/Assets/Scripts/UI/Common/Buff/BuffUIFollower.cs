using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class BuffUIFollower : MonoBehaviour
{
    public Transform target;            // 따라갈 플레이어
    public float rightOffset = 0.5f;    // 화면 기준 오른쪽으로 얼마나
    public float upOffset = 1.0f;       // 위로 얼마나
    public float forwardOffset = 0f;    // 필요하면 살짝 앞/뒤

    private Camera _cam;

    private void Awake()
    {
        _cam = Camera.main;
    }

    private void LateUpdate()
    {
        if (target == null || _cam == null)
            return;

        // 위치: 월드 기준
        Vector3 worldPos =
            target.position +
            Vector3.right * rightOffset +
            Vector3.up * upOffset;
        
        transform.position = worldPos;
        transform.rotation = _cam.transform.rotation;
    }

    public void SetTarget(Transform t)
    {
        target = t;
    }
}