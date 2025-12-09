using Google.Protobuf.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PlayerViewController : MonoBehaviour
{
    private NavMeshAgent _agent;
    private Animator _animator;
    private MyPlayerController _player;
    private PlayerSkillController _skill;

    private bool _syncing;

    private int _targetId;

    // Moving (For. Target)
    private int _followTargetId = 0;
    private Coroutine _coFollow;

    // Attack
    private GameObject _target;

    public HashSet<int> VisibleObjectIds { get; set; } = new HashSet<int>();
    public HashSet<int> WardIds { get; set; } = new HashSet<int>();

    public int TargetId { get { return _targetId; } set { _targetId = value; } }

    private void Awake()
    {
        _agent = GetComponentInChildren<NavMeshAgent>();
        _animator = GetComponentInChildren<Animator>();
        _player = GetComponentInChildren<MyPlayerController>();
        _skill = GetComponentInChildren<PlayerSkillController>();
    }

    private void Update()
    {
        if (!_syncing || _agent == null || _player == null)
            return;

        if (_player.State == CreatureState.Attack)
        {
            var targetView = Managers.Object.FindById(TargetId);
            if (targetView != null)
            {
                Vector3 pos = targetView.transform.position;
                UpdateTarget(pos);
            }
        }
        else
             _player.UpdateTransform();
    }

    public void OnMove(S_Move packet)
    {
        if (_player.State == CreatureState.Skill)
            return;
    }

    public void OnMoveSync(S_MoveSync packet)
    {
        C_SetMoveTarget cmd = new C_SetMoveTarget()
        {
            IsGround = true,
            TargetPos = packet.TargetPos,
        };
        ApplyLocalSetMoveTarget(cmd, true);
    }

    public void OnAnim(S_Anim packet)
    {
    }

    public void OnHpChanged(S_ChangeHp packet)
    {
    }

    public void OnRest(S_Rest packet)
    {
        UI_InteractionCharge ic = _player.UI.InteractionCharge;
        if (packet.IsRest)
            ic.Begin(packet.Duration, "휴식 준비 중");
        else
            ic.Begin(packet.Duration, "휴식 해제 중");
    }

    public void OnDead(S_Die packet)
    {
    }

    public S_Die GetRestCommand()
    {
        return new S_Die();
    }

    public void OnRespawn(S_Respawn packet)
    {
        _agent.enabled = false;

        Vector3 respawnPos = new Vector3(packet.PosInfo.PosX, packet.PosInfo.PosY, packet.PosInfo.PosZ);
        transform.position = respawnPos;

        _agent.enabled = true;
        _agent.Warp(respawnPos);

        _player.Hp = packet.Hp;
        _player.Stamina = packet.Stamina;
        _player.IsRest = packet.IsRest;

        _player.UpdateTransform(true);
    }

    public void OnStop(S_Stop packet)
    {
        ApplyStop(packet.Reason);
    }

    #region Moving
    public void ApplyLocalSetMoveTarget(C_SetMoveTarget cmd, bool isServerSync = false, float attackRange = 3.0f)
    {
        if (_agent == null)
            return;

        if (_player.AllowOffPathMovement)
            return;

        if (_player.State == CreatureState.Skill && !_skill.CanMoveDuringCast)
            return;
        else
            _skill.StopSkillMotion();

        //_agent.speed = _player.Speed;

        // 추적 코루틴 정리
        StopFollowTarget();

        _agent.enabled = true;
        _agent.updatePosition = true;     
        _agent.updateRotation = true;     
        _agent.isStopped = false;

        if (cmd.IsGround)
        {
            // 땅 이동
            Vector3 final = new Vector3(cmd.TargetPos.PosX, cmd.TargetPos.PosY, cmd.TargetPos.PosZ);
            if (NavMesh.SamplePosition(final, out var navHit, 2.0f, NavMesh.AllAreas))
                final = navHit.position;

            if(_agent != null)
                _agent.SetDestination(final);
        }
        else
        {
            // 타겟팅 이동: 타겟 현재 위치를 주기적으로 따라간다
            _followTargetId = cmd.TargetId;

            // 즉시 한 번 갱신 후, 주기 추적 시작
            UpdateFollowDestinationOnce();
            _coFollow = StartCoroutine(CoFollowTarget(0.01f)); // 0.2~0.3s 주기 권장         
        }

        // MoveSync 루프 스타트(좌표/회전 동기화는 계속 필요)
        _syncing = true;
    }

    // ---- 정지 입력 처리 (S/H) ----
    public void ApplyStop(StopReason reason)
    {
        if (_agent == null || !_agent.isOnNavMesh)
            return;

        switch (reason)
        {
            case StopReason.StopAll:
            case StopReason.StopMoveOnly:
                if (_player.AllowOffPathMovement)
                    return;

                _agent.enabled = true;
                _agent.isStopped = true;

                StopFollowTarget(); // 추적 종료(서버 사인에 의해)
                _agent.ResetPath();
                break;
        }

        // 동기화 루프는 유지(서버에 현재 정지 상태 포지션 계속 보고)
        _player.UpdateTransform(true);
    }

    // ---- 타겟 추적 코루틴 ----
    private IEnumerator CoFollowTarget(float intervalSec)
    {
        var wait = new WaitForSeconds(intervalSec);
        while (_followTargetId != 0 && _agent != null && !_agent.isStopped)
        {
            UpdateFollowDestinationOnce();
            yield return wait;
        }
        _coFollow = null;
    }

    private void UpdateFollowDestinationOnce()
    {
        if (_followTargetId == 0)
            return;

        var targetView = Managers.Object.FindById(_followTargetId);
        if (!_player.IsAttackable(targetView, out var reason))
        {
            // 타겟이 사라졌으면 추적 종료
            SendAttackTargetInvalid(_followTargetId, reason);
            StopFollowTarget();
            return;
        }

        Vector3 myPos = transform.position;
        Vector3 targetPos = targetView.transform.position;
        myPos.y = targetPos.y;

        // GetAttackStopPosition
        Vector3 dir = targetPos - myPos;
        dir.y = 0f;
        float dist = dir.magnitude;
        if (dist <= Mathf.Epsilon)
            return;
        dir /= dist;

        float _stopBuffer = 0.1f;
        //float stop = Mathf.Max(0.05f, _player.AttackRange - _stopBuffer);
        //Vector3 finPos = targetPos - dir * stop;

        Vector3 finPos = myPos;
        float distance = Vector3.Distance(myPos, targetPos);
        if(distance >= _player.AttackRange - _stopBuffer)
            finPos = targetPos - dir * (_player.AttackRange - _stopBuffer);

        if (NavMesh.SamplePosition(finPos, out var navHit, 2.0f, NavMesh.AllAreas))
            finPos = navHit.position;

        _agent.SetDestination(finPos);
    }

    private void StopFollowTarget()
    {
        _followTargetId = 0;
        if (_coFollow != null)
        { StopCoroutine(_coFollow); _coFollow = null; }

        _agent.enabled = true;
        _agent.isStopped = true;
        _agent.ResetPath();
    }
    #endregion

    #region Helper
    public void UpdateTarget(Vector3 targetPos)
    {
        Vector3 dir = targetPos - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f)
            return;

        transform.rotation = Quaternion.LookRotation(dir);
        
        _player.UpdateTransform();
    }

    public void RotateAttack(int targetId)
    {
        _target = Managers.Object.FindById(targetId);
        if (_target == null)
            return;

        StartCoroutine(CoRotateToTarget());
    }

    private IEnumerator CoRotateToTarget()
    {
        const float rotateSpeed = 15f;

        while (true)
        {
            if (_target == null)
                break;

            if (_player.State == CreatureState.Moving)
                break;

            // 타겟 방향 계산
            Vector3 dir = _target.transform.position - transform.position;
            dir.y = 0;

            // 너무 가까우면 회전 중단
            if (dir.sqrMagnitude < 0.01f)
                break;

            Quaternion targetRot = Quaternion.LookRotation(dir);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRot,
                rotateSpeed * Time.deltaTime * 50f
            );

            if (Quaternion.Angle(_player.RotInfo, targetRot) < 0.5f)
                break;

            yield return null;
        }
    }

    private void SendAttackTargetInvalid(int targetId, InvalidTargetReason reason)
    {
        C_AttackTargetInvalid packet = new C_AttackTargetInvalid()
        {
            ObjectId = _player.Id,
            TargetId = targetId,
            Reason = reason
        };

        _player.SendPacket(packet);
    }
    #endregion
}

