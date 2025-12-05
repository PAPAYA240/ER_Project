using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class IO_BaseTrigger : MonoBehaviour
{
    [Header("BaseTrigger Settings")]
    public int side;                   // Inspector에서 선택됨

    private bool _isInside;

    private void OnTriggerEnter(Collider other)
    {
        // MyPlayer만 체크
        var player = other.GetComponentInParent<MyPlayerController>();
        if (player == null)
            return;

        if (_isInside)
            return;

        _isInside = true;
        SendZoneStatePacket(player.Id, true);
    }

    private void OnTriggerExit(Collider other)
    {
        var player = other.GetComponentInParent<MyPlayerController>();
        if (player == null)
            return;

        if (!_isInside) 
            return;

        _isInside = false;
        SendZoneStatePacket(player.Id, false);
    }

    private void SendZoneStatePacket(int id, bool isInside)
    {
        C_BaseTrigger pkt = new C_BaseTrigger();
        pkt.ObjectId = id;
        pkt.Team = side;
        pkt.IsInside = isInside;

        Managers.Network.Send(pkt); 
    }
}

