using Google.Protobuf.Protocol;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : BaseController
{
    public GameObject Owner { get; set; } = null;

    private Vector3 _lastForward;
    private float _elapsedTime = 0f;
    private void Awake()
    {
        gameObject?.SetActive(false);
    }
    public void MoveHandler(S_Move movePacket)
    {
        gameObject?.SetActive(true);
        PosInfo = movePacket.PosInfo;
        RotInfo = movePacket.RotInfo;
        SyncPos(movePacket.IsWarp);

        _elapsedTime += Time.deltaTime;
        if(_elapsedTime >= 2.0f)
            Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        // 스크린 활용 시 모든 몬스터와 플레이어도 맞게 할 수 있음
        if (other.gameObject.layer == LayerMask.NameToLayer("Monster"))
        {
            CreatureController targetController = other.GetComponent<CreatureController>();
            PlayerController ownerController = Owner.GetComponent<PlayerController>();

            List<CreatureController> hitList = new List<CreatureController>();
            hitList.Add(targetController);
            //ownerController.LaunchProjectile(hitList);

            Destroy(gameObject);
        }
    }
}
