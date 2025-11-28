using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using NUnit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class Projectile_Rozzi_NormalAttack : Projectile
{
    private int _targetId = 0;

    private bool _isLWeapon = true;
    private string _LWeaponBone = "WP_Rozzi_S002_L_Pistol";
    private string _RWeaponBone = "WP_Rozzi_S002_R_Pistol";

    private float _speed = 1f;
    private float _hitRadius = 0.5f;

    bool _hasHit = false;     // 실제 히트 발생 여부 (Update/이동 막기)
    bool _packetSent = false; // 서버로 C_RozziNormalAttack 보낸 적 있는지

    // 풀에서 꺼낼 때 항상 호출해줄 리셋 함수
    public void ResetForPool()
    {
        _targetId = 0;
        _speed = 1f;
        _hasHit = false;
        _packetSent = false;
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
        if (_hasHit || _targetId == 0)
            return;

        GameObject target = Managers.Object.FindById(_targetId);
        if(target == null) 
            return;

        transform.position = Vector3.MoveTowards(transform.position, target.transform.position, _speed * Time.deltaTime);
        float dist = Vector2.Distance(  new Vector2(transform.position.x, transform.position.z), 
                                        new Vector2(target.transform.position.x, target.transform.position.z));

        if (dist <= _hitRadius)
            OnHit();
    }

    private void SetStartPosition()
    {
        Transform boneTransform = Util.FindChildByName(Owner.transform, _isLWeapon ? _LWeaponBone : _RWeaponBone).transform;
        if (boneTransform == null)
            return;

        transform.position = boneTransform.position;
        Debug.Log($"@ bone : {(_isLWeapon ? _LWeaponBone : _RWeaponBone)}, pos - {transform.position}");
    }

    private void OnHit()
    {
        if (_hasHit)
            return;        
        _hasHit = true;

        gameObject.SetActive(false);
        TrySendHitPacket();

        Debug.Log($"@ OnHit!!");
    }

    private void TrySendHitPacket()
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
            TargetId = _targetId
        };
        Managers.Network.Send(packet);
        Debug.Log("@ Send C_RozziNormalAttack");
    }
}
