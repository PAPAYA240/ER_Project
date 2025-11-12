using Google.Protobuf.Protocol;
using UnityEngine;

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
        // 스크린 활용 시 모든 몬스터와 플레이어도 맞게 할 수 있음
        bool bDestroy = false;

        CreatureController cc = other.gameObject.GetComponentInChildren<CreatureController>();
        if (cc == null) return;

        GameObjectType objectType = ObjectManager.GetObjectTypeById(cc.Id);
        if (objectType == GameObjectType.Player)
        {
            PlayerController player = other.gameObject.GetComponent<PlayerController>();
            PlayerController ownerPlayer = Owner.GetComponent<PlayerController>();

            bDestroy = (ownerPlayer.ObjInfo.Player.Team != player.ObjInfo.Player.Team);
        }
        else if (objectType == GameObjectType.Monster)
        {
            bDestroy = other.gameObject.layer == LayerMask.NameToLayer("Monster");
        }

        if(bDestroy)
        {
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
