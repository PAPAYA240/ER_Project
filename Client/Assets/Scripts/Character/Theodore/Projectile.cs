using Google.Protobuf.Protocol;
using UnityEngine;
using static Define;

public class Projectile : BaseController
{
    public GameObject Owner { get; set; } = null;
    public ProjectileType Type { get; set; }
    private Vector3 _lastForward;

    void Update()
    {

    }
    void OnTriggerEnter(Collider other)
    {
    }

}
