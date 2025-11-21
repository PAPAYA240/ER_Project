using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class Projectile_Rozzi : Projectile
{
    private BOMB_ROZZI _state = BOMB_ROZZI.NoneBomb;

    void Update()
    {
    }


    void OnTriggerEnter(Collider other)
    {
        if (Type == ProjectileType.ProjectileRozziR)
            return;
    }

    public void ChangeState(S_ProjectileRozzi packet)
    {
        _state = packet.State;
        switch (_state)
        {
            case BOMB_ROZZI.Flying:

                break;
            case BOMB_ROZZI.AttachedToTarget:   // 대상에게 부착!
                GameObject target = Managers.Object.FindById(packet.TargetId);

                break;
            case BOMB_ROZZI.StuckOnGround:

                break;
            case BOMB_ROZZI.Exploded:   // 폭발

                break;
            default:

                break;
        }
    }
}
