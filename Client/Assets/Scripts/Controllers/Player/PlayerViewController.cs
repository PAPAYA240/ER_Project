using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

public class PlayerViewController : MonoBehaviour
{
    private NavMeshAgent _agent;
    private Animator _animator;
    private MyPlayerController _player;

    private bool _syncing;
    private bool _sentArriveSnapshot;

    [SerializeField] private float _minMoveDelta = 0.01f; // 1cm
    [SerializeField] private float _minAngleDelta = 1.0f; // 1도

    private Vector3 _lastSentPos;
    private Quaternion _lastSentRot;

    // 타겟 추적용
    public HashSet<int> VisibleObjectIds { get; set; } = new HashSet<int>();
    private int _followTargetId = 0;
    private Coroutine _coFollow;

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

        _player.UpdateTransform();

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

        Vector3 pos = _player.transform.position;
        Quaternion rot = _player.transform.rotation;

        if ((pos - _lastSentPos).sqrMagnitude >= _minMoveDelta * _minMoveDelta ||
            Quaternion.Angle(rot, _lastSentRot) >= _minAngleDelta)
        {
            _lastSentPos = pos;
            _lastSentRot = rot;
            _sentArriveSnapshot = false;
        
            _player.UpdateTransform();
        }
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

    #region State
    public void ApplyState(S_PlayerState packet)
    {
        CreatureState state = packet.State;
        switch (state)
        {
            case CreatureState.Idle:
                ApplyStop(StopReason.StopAll);
                break;
            case CreatureState.Moving:

                break;
            case CreatureState.Attack:
                ApplyStop(StopReason.StopAll);
                break;
        }
    }
    #endregion

    #region Moving
    public void ApplyLocalSetMoveTarget(C_SetMoveTarget cmd, float attackRange = 3.0f)
    {
        if (_agent == null)
            return;

        // 추적 코루틴 정리
        StopFollowTarget();

        if (cmd.IsGround)
        {
            // 땅 이동
            Vector3 final = new Vector3(cmd.TargetPos.PosX, cmd.TargetPos.PosY, cmd.TargetPos.PosZ);
            if (NavMesh.SamplePosition(final, out var navHit, 2.0f, NavMesh.AllAreas))
                final = navHit.position;

            _agent.stoppingDistance = 0.05f;
            _agent.isStopped = false;
            _agent.SetDestination(final);
        }
        else
        {
            // 타겟팅 이동: 타겟 현재 위치를 주기적으로 따라간다
            _followTargetId = cmd.TargetId;
            float stopDist = Mathf.Max(0.05f, attackRange * 0.9f); // 살짝 여유를 줘서 덜 출렁이게
            _agent.stoppingDistance = stopDist;
            _agent.isStopped = false;

            // 즉시 한 번 갱신 후, 주기 추적 시작
            UpdateFollowDestinationOnce();
            _coFollow = StartCoroutine(CoFollowTarget(0.01f)); // 0.2~0.3s 주기 권장
        }

        // MoveSync 루프 스타트(좌표/회전 동기화는 계속 필요)
        _syncing = true;
        _lastSentPos = _player.transform.position;
        _lastSentRot = _player.transform.rotation;
    }

    // ---- 정지 입력 처리 (S/H) ----
    public void ApplyStop(StopReason reason)
    {
        if (_agent == null)
            return;

        switch (reason)
        {
            case StopReason.StopAll:
                // 이동 정지 + 추적 취소
                _agent.isStopped = true;
                StopFollowTarget();
                // (공격 정지는 서버 상태머신이 처리. 여기선 이동만)
                break;

            case StopReason.StopMoveOnly:
                // 이동만 정지, 추적 금지
                _agent.isStopped = true;
                StopFollowTarget(); // 추적도 중단(클라 측 추격 금지)
                break;
        }

        // 동기화 루프는 유지(서버에 현재 정지 상태 포지션 계속 보고)
        _syncing = true;
        _lastSentPos = _player.transform.position;
        _lastSentRot = _player.transform.rotation;
    }

    // ---- 타겟 추적 코루틴 ----
    private System.Collections.IEnumerator CoFollowTarget(float intervalSec)
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

        // TEMP : 나중에 Target 상태 체크해서 쫓아갈지 검사
        //var targetView = FindVisibleObjectById(_followTargetId);
        var targetView = Managers.Object.FindById(_followTargetId);
        if (targetView == null)
        {
            // 타겟이 사라졌으면 추적 종료
            StopFollowTarget();
            return;
        }

        Vector3 pos = targetView.transform.position;
        if (NavMesh.SamplePosition(pos, out var navHit, 2.0f, NavMesh.AllAreas))
            pos = navHit.position;

        _agent.SetDestination(pos);
    }

    private void StopFollowTarget()
    {
        _followTargetId = 0;
        if (_coFollow != null)
        { StopCoroutine(_coFollow); _coFollow = null; }

        _agent.isStopped = true;
        _agent.ResetPath();              
    }
    #endregion

    #region Helper
    private GameObject FindVisibleObjectById(int objectId)
    {
        if (VisibleObjectIds.Contains(objectId))
        {
            return Managers.Object.FindById(objectId);
        }

        return null;
    }
    #endregion
}

