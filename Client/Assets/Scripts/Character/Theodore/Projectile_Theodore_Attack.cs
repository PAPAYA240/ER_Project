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
    private float _speed = 1f;

    private float _deltaScale = 0.07f;
    private float _elapsed = 0f;
    private float _maxTravelTime = 1f;
    public void Init(S_TheodoreAttack packet)
    {
        _targetId = packet.TargetId;
        _speed = packet.Speed;

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
        //else
        //{
        //    Transform boneTransform = Util.FindChildByName(Owner.transform, "Fx_Hand_L").transform;
        //    if (boneTransform == null)
        //        return;
        //    CellPos = boneTransform.position;
        //    SyncPos();
        //    _dashStartPos = transform.position;
        //}
    }
    private void Update()
    {
        if (Type == ProjectileType.ProjectileTheodoreNormalAttack)
            AttackTarget();
    }


    private void AttackForward()
    {
        //Vector3 finPos;
        //Quaternion finRot = transform.rotation; 

        //float totalDistance = _speed * 1.5f * _deltaScale;

        //Vector3 targetPos = _dashStartPos + transform.forward * totalDistance;
        //targetPos.y = _dashStartPos.y; // Y축 고정

        //_elapsed += _speed * Time.deltaTime * _deltaScale;

        //if (_elapsed >= 1.5f)
        //{
        //    finPos = targetPos; 
        //}
        //else
        //{
        //    finPos = Vector3.Lerp(_dashStartPos, targetPos, _elapsed / 1.5f);
        //}

        //finRot = transform.rotation;

        //CellPos = finPos;
        //RotInfo = finRot;
        //SyncPos();
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
        finRot = Quaternion.LookRotation(dir, Vector3.up);

        // Sync
        CellPos = finPos;
        RotInfo = finRot;
        SyncPos();
    }
}
