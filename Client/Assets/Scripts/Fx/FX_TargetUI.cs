using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class FX_TargetUI : MonoBehaviour
{
    public Transform Target;
    public Vector3 Offset;              // 타겟 기준 world offset
    public bool FaceCamera = false;     // 카메라 바라보게 할지
    public Quaternion FixedRotation;    // 항상 유지할 고정 회전값

    // 외부에서 한 번 설정해주는 함수
    public void Setup(Transform target, Vector3 offset, Quaternion fixedRot, bool faceCamera = false)
    {
        Target = target;
        Offset = offset;
        FixedRotation = fixedRot;
        FaceCamera = faceCamera;
    }

    void LateUpdate()
    {
        if (Target == null)
            return;

        // 1) 위치만 타겟을 따라감
        transform.position = Target.position + Offset;

        // 2) 회전은 항상 동일한 값 사용
        if (FaceCamera && Camera.main != null)
        {
            // UI 느낌으로 카메라를 정면으로 바라보게 하고 싶으면 이쪽
            transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
        }
        else
        {
            // 완전히 고정 회전
            transform.rotation = FixedRotation;
        }
    }
}