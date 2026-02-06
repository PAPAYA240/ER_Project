using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class PingController
{
    private PlayerController _owner;
    private float[] _pingTimes = new float[8];
    private float _cooldown = 3f;

    public PingController(PlayerController owner)
    {
        _owner = owner;
        for (int i = 0; i < 8; i++)
            _pingTimes[i] = -Mathf.Infinity;
    }

    public bool TryPlacePing(Vector3 pos)
    {
        int idx = GetSlot();
        if (idx == -1)
            return false;

        _pingTimes[idx] = Time.time;
        PlayPing(pos, isMyPlayer: true); 
        return true;
    }

    public void PlayPing(Vector3 pos, bool isMyPlayer = false)
    {
        if(isMyPlayer)
        {
            if(_owner is MyPlayerController mpc)
            {
                C_PingMarker packet = new C_PingMarker()
                {
                    ObjectId = _owner.Id,
                    TargetPos = pos
                };
                mpc.SendPacket(packet);
            }
        }

        _owner.Effect.PlayEffect(commonName: "Ping", mousePos: pos, targetPos: default, targetRot: default);
        Managers.Object.MyPlayer.Sound.GetEffect3D("Ping", pos);
    }

    private int GetSlot()
    {
        float now = Time.time;
        for (int i = 0; i < 8; i++)
            if (now - _pingTimes[i] >= _cooldown)
                return i;

        return -1;
    }   
}
