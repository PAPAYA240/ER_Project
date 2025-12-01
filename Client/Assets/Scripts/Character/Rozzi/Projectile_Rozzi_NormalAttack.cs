using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using NUnit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class Projectile_Rozzi_NormalAttack : Projectile
{
    private int _targetId = 0;

    private bool _isLWeapon = true;
    private string _LWeaponBone = "WP_Rozzi_S002_L_Pistol";
    private string _RWeaponBone = "WP_Rozzi_S002_R_Pistol";

    private float _speed = 1f;
    private float _hitRadius = 0.5f;

    private bool _hasHit = false;     // 실제 히트 발생 여부 (Update/이동 막기)
    private bool _packetSent = false; // 서버로 C_RozziNormalAttack 보낸 적 있는지

    private float _maxTravelTime = 1f;
    private float _deltaScale = 0.05f;

    private float _elapsed = 0f;

    // 풀에서 꺼낼 때 항상 호출해줄 리셋 함수
    public void ResetForPool()
    {
        _targetId = 0;
        _speed = 1f;
        _hasHit = false;
        _packetSent = false;
        _elapsed = 0f;
    }

    public void Init(S_RozziNormalAttack packet)
    {
        ResetForPool();   // 재사용 대비

        _targetId = packet.TargetId;
        _isLWeapon = packet.IsLWeapon;
        _speed = packet.Speed;

        SetStartPosition();
    }

    private void Update()
    {
        GameObject target = Managers.Object.FindById(_targetId);
        if (_hasHit ||_targetId == 0 || target == null)
        {
            OnHit(false);
            return;
        }

        Vector3 finPos = transform.position;
        Quaternion finRot = transform.rotation;

        // Position
        _elapsed += _speed * Time.deltaTime * _deltaScale;
        if (_elapsed >= _maxTravelTime)
            finPos = target.transform.position;
        else
            finPos = Vector3.Lerp(transform.position, target.transform.position, _elapsed / _maxTravelTime);

        // Rotation
        Vector3 dir = target.transform.position - transform.position;
        finRot = Quaternion.LookRotation(dir, Vector3.up);

        // Sync
        CellPos = finPos;
        RotInfo = finRot;
        SyncPos();

        // Distance
        float dist = Vector2.Distance(new Vector2(transform.position.x, transform.position.z),
                                        new Vector2(target.transform.position.x, target.transform.position.z));

        if (dist <= _hitRadius)
            OnHit(true);
    }

    private void SetStartPosition()
    {
        Transform boneTransform = Util.FindChildByName(Owner.transform, _isLWeapon ? _LWeaponBone : _RWeaponBone).transform;
        if (boneTransform == null)
            return;

        transform.position = boneTransform.position;
        CellPos = boneTransform.position;
        SyncPos();  
    }

    private void OnHit(bool hasHit)
    {
        if (_hasHit)
            return;        
        _hasHit = true;

        gameObject.SetActive(false);
        TrySendHitPacket(hasHit);
    }

    private void TrySendHitPacket(bool hasHit)
    {
        // 1) 내 플레이어 아닌 경우 → 서버에 패킷 안 보냄 (단순 이펙트 전용)
        var myPlayer = Managers.Object.MyPlayer;
        if (myPlayer == null)
            return;

        if (Owner == null || Owner.gameObject != myPlayer.gameObject)
            return;

        // 2) 이미 한 번 보냈으면 또 안 보냄
        if (_packetSent)
            return;
        _packetSent = true;

        C_RozziNormalAttack packet = new C_RozziNormalAttack
        {
            ObjectId = Id,
            TargetId = _targetId,
            HasHit = hasHit
        };
        Managers.Network.Send(packet);
    }
}
