using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class WorldUIBillboard : MonoBehaviour
{
    private Camera _cam;

    private void Awake()
    {
        _cam = Camera.main;
    }

    private void LateUpdate()
    {
        if (_cam == null)
            return;

        // UI가 카메라 방향을 바라보도록 회전
        Vector3 dir = transform.position - _cam.transform.position;
        transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }
}
