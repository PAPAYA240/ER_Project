using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class Fx_FollowEffect : MonoBehaviour
{
    private Transform _target;
    private Vector3 _localPosition;
    private Quaternion _localRotation; // data.rotation을 저장합니다.

    public void Setup(Transform target, Vector3 localPosition, Quaternion localRotation)
    {
        _target = target;
        _localPosition = localPosition;
        _localRotation = localRotation;
    }
  
    private void LateUpdate()
    {
        if (_target == null)
            return;

        transform.position = _target.position + _target.rotation * _localPosition;

        Quaternion baseRot = _target.rotation;
        Vector3 euler = _localRotation.eulerAngles;
        if (euler.magnitude > 0.01f)
            baseRot = _target.rotation * _localRotation;

        transform.rotation = baseRot;
    }
}
