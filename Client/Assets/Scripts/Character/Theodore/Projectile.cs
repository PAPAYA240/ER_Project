using Google.Protobuf.Protocol;
using UnityEngine;

public class Projectile : BaseController
{
    public GameObject Owner { get; set; } = null;

    private Vector3 _lastForward;
    private float _elapsedTime = 0f;

    void Update()
    {
    }


    void OnTriggerEnter(Collider other)
    {
        // 스크린 활용 시 모든 몬스터와 플레이어도 맞게 할 수 있음
        if (other.gameObject.layer == LayerMask.NameToLayer("Monster"))
        {
            Destroy(gameObject);
        }
    }
}
