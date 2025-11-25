using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class SpawnPointMarker : MonoBehaviour
{
    [Header("Spawn Point Settings")]
    public int id;

    public bool side;                   // Inspector에서 선택됨
    public SpawnPointType type;         // Inspector에서 선택됨

    [Tooltip("지정하면 이 Transform 위치를 Spawn 지점으로 사용합니다.")]
    public Transform overrideSpawnPoint;

    public Vector3 GetPosition()
        => overrideSpawnPoint != null ? overrideSpawnPoint.position : transform.position;
}