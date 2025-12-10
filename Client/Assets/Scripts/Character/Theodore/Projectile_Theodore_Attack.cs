using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class Projectile_Theodore_Attack : Projectile
{
    private int _targetId = 0;
    private float _speed = 10f;

    private float _deltaScale = 0.07f;
    private float _elapsed = 0f;
    private float _maxTravelTime = 1f;

    public void Init(S_TheodoreAttack packet)
    {
        _targetId = packet.TargetId;
        _speed = 15.0f/*packet.Speed*/;

        SetStartPosition();
    }
    private void SetStartPosition()
    {
        if (Type == ProjectileType.ProjectileTheodoreNormalAttack)
        {
            Transform boneTransform = Util.FindChildByName(Owner.transform, "ShotPoint").transform;
            if (boneTransform == null)
                return;
            CellPos = boneTransform.position;
            SyncPos();
        }
    }
    private void Update()
    {
        if (Type == ProjectileType.ProjectileTheodoreNormalAttack)
            AttackTarget();
    }

    private void AttackTarget()
    {
        GameObject target = Managers.Object.FindById(_targetId);
        if (target == null)
            return;

        Vector3 finPos = transform.position;
        Quaternion finRot = transform.rotation;

        Vector3 targetPos = target.transform.position;
        targetPos.y = transform.position.y;

        // Position
        _elapsed += _speed * Time.deltaTime * _deltaScale;
        if (_elapsed >= _maxTravelTime)
            finPos = targetPos;
        else
            finPos = Vector3.Lerp(transform.position, targetPos, _elapsed / _maxTravelTime);

        // Rotation
        Vector3 dir = targetPos - transform.position;
        if (dir.sqrMagnitude > 0.001f)  
            finRot = Quaternion.LookRotation(dir, Vector3.up);
        else
            finRot = transform.rotation;  

        // Sync
        CellPos = finPos;
        RotInfo = finRot;
        SyncPos();
    }
}
