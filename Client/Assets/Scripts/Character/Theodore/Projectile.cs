using Google.Protobuf.Protocol;
using UnityEngine;
using static Define;

public class Projectile : BaseController
{
    public GameObject Owner { get; set; } = null;
    public ProjectileType Type { get; set; }
    private Vector3 _lastForward;

    void Start()
    {
        if (Type == ProjectileType.ProjectileTheodoreE)
        {
            PlayerController player = Owner.GetComponent<PlayerController>();
            player.PlaySelectEffect(KeyCode.E, default(Vector3), default(Vector3), Quaternion.identity, "FX_Skill03_Shield", this.transform);
        }
    }

    void OnDestroy()
    {
        if (Type == ProjectileType.ProjectileTheodoreE)
        {
            PlayerController player = Owner.GetComponent<PlayerController>();
            GameObject effect = Managers.FX.Effect.FindCurrentPlayEffect(player.Id, "FX_Skill03_Shield");
            if (effect != null)
                Managers.FX.Effect.RemoveEffect(player.Id, effect);
        }
    }
    void OnTriggerEnter(Collider other)
    {
    }

}
