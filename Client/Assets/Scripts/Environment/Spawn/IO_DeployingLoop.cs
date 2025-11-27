using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class IO_DeployingLoop : MonoBehaviour
{
    [Header("DeployingLoop Settings")]
    public int id;
    public bool side;                   // Inspector에서 선택됨

    private bool _isPlayerInside = false;
    public bool IsPlayerInside => _isPlayerInside;

    [SerializeField]
    private Transform _lookTarget;  // Cobalt_OBJ_DeployingLoop_Grass 할당용

    public Vector3 GetLookTargetPosition()
    {
        return _lookTarget != null ? _lookTarget.position : transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInChildren<MyPlayerController>() != null)
        {
            _isPlayerInside = true;
            // Hint UI 켜기, 상호작용 키 안내 등
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInChildren<MyPlayerController>() != null)
        {
            _isPlayerInside = false;
            // Hint UI 끄기
        }
    }
}

