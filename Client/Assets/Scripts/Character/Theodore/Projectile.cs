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
        if (Type == ProjectileType.ProjectileBullet)
        {
            Quaternion currentRot = RotInfo;
            Vector3 euler = currentRot.eulerAngles;
            euler.x = 90f;
            transform.rotation = Quaternion.Euler(euler);
        }
    }
    void OnTriggerEnter(Collider other)
    {
    }

}
