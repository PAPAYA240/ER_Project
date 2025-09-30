using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.GraphicsBuffer;

public class PlayerViewController : MonoBehaviour
{
    private NavMeshAgent _agent;
    private Animator _animator;
    private MyPlayerController _player;

    private bool _syncing;
    private bool _sentArriveSnapshot;

    [SerializeField] private float _syncInterval = 0.05f; // 20Hz
    [SerializeField] private float _minMoveDelta = 0.03f; // 3cm
    [SerializeField] private float _minAngleDelta = 1.0f; // 1도
    private float _lastSyncAt;
    private Vector3 _lastSentPos;
    private Quaternion _lastSentRot;

    private void Awake()
    {
        _agent = GetComponentInChildren<NavMeshAgent>();
        _animator = GetComponentInChildren<Animator>();
        _player = GetComponentInChildren<MyPlayerController>();
    }

    private void Update()
    {
        if (!_syncing || _agent == null || _player == null)
            return;

        // 1) 도착/정지 감지
        bool arrived =
            !_agent.pathPending &&
            _agent.remainingDistance <= _agent.stoppingDistance &&
            (_agent.hasPath == false || _agent.velocity.sqrMagnitude < 0.0001f);

        if (arrived)
        {
            if (!_sentArriveSnapshot)
            {
                _player.UpdateTransform();
                _sentArriveSnapshot = true;
            }
            _syncing = false;
            return;
        }

        // 2) 주기/임계치 충족 시에만 송신 (밴드폭 절약)
        if (Time.time - _lastSyncAt < _syncInterval)
            return;

        Vector3 pos = _player.transform.position;
        Quaternion rot = _player.transform.rotation;

        if ((pos - _lastSentPos).sqrMagnitude >= _minMoveDelta * _minMoveDelta ||
            Quaternion.Angle(rot, _lastSentRot) >= _minAngleDelta)
        {
            _player.UpdateTransform();

            _lastSentPos = pos;
            _lastSentRot = rot;
            _lastSyncAt = Time.time;
            _sentArriveSnapshot = false;
        }
    }

    // 입력으로 받은 C_Move를 "즉시 로컬 반영" + 동기화 루프 시작
    public void ApplyLocalMove(C_Move cmd, float attackRange = 3.0f)
    {
        if (_agent == null)
            return;

        Vector3 final = new Vector3 { x = cmd.TargetPosition.PosX, y = cmd.TargetPosition.PosY, z = cmd.TargetPosition.PosZ };

        if (NavMesh.SamplePosition(final, out var navHit, 2.0f, NavMesh.AllAreas))
            final = navHit.position;

        _agent.stoppingDistance = 0.05f;
        _agent.SetDestination(final);

        // MoveSync 루프 스타트
        _syncing = true;
        _lastSentPos = _player.transform.position;
        _lastSentRot = _player.transform.rotation;
        _lastSyncAt = 0f;
    }

    public void OnMove(S_Move packet)
    {
        Vector3 serverPos = new Vector3(packet.PosInfo.PosX, packet.PosInfo.PosY, packet.PosInfo.PosZ);
        if (Vector3.SqrMagnitude(serverPos - _player.transform.position) > 1.0f) // 1m 이상 벌어지면 보정
        {
            if (NavMesh.SamplePosition(serverPos, out NavMeshHit navHit, 2.0f, NavMesh.AllAreas))
            {
                _agent.Warp(navHit.position);
                _player.UpdateTransform(true);
            }
        }
    }

    public void OnSkill(S_Skill packet)
    {
        if(packet.CanUse)
        {
            //_animator.SetTrigger("Skill" + (KeyCode)packet.SkillInfo.KeyCode);
        }
    }

    public void OnAnim(S_Anim packet)
    {
        //_animator.SetTrigger(packet.AnimInfo.AnimName);
    }

    public void OnHpChanged(S_ChangeHp packet)
    {
        // TODO: HP bar UI
    }

    public void OnDead(S_Die packet)
    {
        //_animator.SetTrigger("Die");
    }

    public void OnRespawn(S_Respawn packet)
    {
        _agent.Warp(new Vector3(packet.PosInfo.PosX, packet.PosInfo.PosY, packet.PosInfo.PosZ));
        _player.UpdateTransform(true);
        _syncing = false; // 리스폰 직후는 입력 올 때까지 동기화 중지
    }
}

