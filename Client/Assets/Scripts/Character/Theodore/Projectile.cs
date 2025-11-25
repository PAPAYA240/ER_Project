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
        if (Type == ProjectileType.ProjectileRozziR)
            return;

        // 스크린 활용 시 모든 몬스터와 플레이어도 맞게 할 수 있음
        bool bDestroy = false;

        CreatureController cc = other.gameObject.GetComponentInChildren<CreatureController>();
        if (cc == null) return;

        GameObjectType objectType = ObjectManager.GetObjectTypeById(cc.Id);
        PlayerController player = other.gameObject.GetComponent<PlayerController>();
        PlayerController ownerPlayer = Owner.GetComponent<PlayerController>();

        if (objectType == GameObjectType.Player)
        {
            bDestroy = (ownerPlayer.ObjInfo.Player.Team != player.ObjInfo.Player.Team);
        }
        else if (objectType == GameObjectType.Monster)
        {
            MonsterController mc = other.gameObject.GetComponentInChildren<MonsterController>();
            if (mc == null)
                return;

            bDestroy = (mc.State != CreatureState.Dead);
        }

        if(bDestroy)
        {
            if (ownerPlayer.Sound != null)
                ownerPlayer.Sound.GetEffect($"Hit_{Type}");

            Destroy(gameObject);

            if (Type == ProjectileType.ProjectileBullet)
                SendAttackPacket(other.gameObject);
        }
    }

    private void SendAttackPacket(GameObject targetObj)
    {
        PlayerController player = Owner.GetComponent<PlayerController>();
        CreatureController target = targetObj.GetComponentInChildren<CreatureController>();
        if (player == null || target == null)
            return;

        C_AttackRequest attackPacket = new C_AttackRequest
        {
            ObjectId = player.Id,
            TargetId = target.Id
        };
        Managers.Network.Send(attackPacket);
    }
}
