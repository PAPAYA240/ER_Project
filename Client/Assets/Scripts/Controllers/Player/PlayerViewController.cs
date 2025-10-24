using Google.Protobuf.Protocol;
using System;
using System.Collections;
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
    private PlayerSkillController _skill;

    private bool _syncing;
    //private bool _sentArriveSnapshot;

    [SerializeField] private float _minMoveDelta = 0.01f; // 1cm
    [SerializeField] private float _minAngleDelta = 1.0f; // 1도

    private Vector3 _lastSentPos;
    private Quaternion _lastSentRot;

    // Moving (For. Target)
    private int _followTargetId = 0;
    private Coroutine _coFollow;

    // Attack
    private int _targetId;

    public HashSet<int> VisibleObjectIds { get; set; } = new HashSet<int>();

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

        //Vector3 pos = _player.transform.position;
        //Quaternion rot = _player.transform.rotation;

        //if ((pos - _lastSentPos).sqrMagnitude >= _minMoveDelta * _minMoveDelta ||
        //    Quaternion.Angle(rot, _lastSentRot) >= _minAngleDelta)
        //{
        //    _lastSentPos = pos;
        //    _lastSentRot = rot;
        //    _sentArriveSnapshot = false;

        //    _player.UpdateTransform();
        //}

        _player.UpdateTransform();

        if (_player.State == CreatureState.Attack)
        {
            var targetView = Managers.Object.FindById(_targetId);
            // TEMP
            if (targetView != null)
            {
                Vector3 pos = targetView.transform.position;
                UpdateTarget(pos);
            }
        }
        else if (_player.State == CreatureState.Moving || _player.State == CreatureState.Idle)
        {
            _player.UpdateTransform();
        }
    }

    public void OnMove(S_Move packet)
    {
        if (_player.State == CreatureState.Skill)
            return;

        //Vector3 serverPos = new Vector3(packet.PosInfo.PosX, packet.PosInfo.PosY, packet.PosInfo.PosZ);
        //if (Vector3.SqrMagnitude(serverPos - _player.transform.position) > 1.0f) // 1m 이상 벌어지면 보정
        //{
        //    if (NavMesh.SamplePosition(serverPos, out NavMeshHit navHit, 2.0f, NavMesh.AllAreas))
        //    {
        //        _agent.Warp(navHit.position);
        //        _player.UpdateTransform(true);
        //    }
        //}
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

    public S_Die GetRestCommand()
    {
        return new S_Die();
    }

    public void OnRespawn(S_Respawn packet)
    {
        _agent.Warp(new Vector3(packet.PosInfo.PosX, packet.PosInfo.PosY, packet.PosInfo.PosZ));
        _player.Hp = packet.Hp;
        _player.UpdateTransform(true);
        _syncing = false; // 리스폰 직후는 입력 올 때까지 동기화 중지
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

        if (_player.State == CreatureState.Skill && !isServerSync)
            return;
        else
            _skill.StopSkillMotion();

        // 추적 코루틴 정리
        StopFollowTarget();

        _agent.enabled = true;
        _agent.updatePosition = true;     // ★ 추가
        _agent.updateRotation = true;     // ★ 추가
        _agent.isStopped = false;

        if (cmd.IsGround)
        {
            // 땅 이동
            Vector3 final = new Vector3(cmd.TargetPos.PosX, cmd.TargetPos.PosY, cmd.TargetPos.PosZ);
            if (NavMesh.SamplePosition(final, out var navHit, 2.0f, NavMesh.AllAreas))
                final = navHit.position;

            _agent.isStopped = false;
            _agent.SetDestination(final);
        }
        else
        {
            // 타겟팅 이동: 타겟 현재 위치를 주기적으로 따라간다
            _followTargetId = cmd.TargetId;
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
        // TEMP
        //if (_motionCo != null)
        //    return;

        if (_agent == null)
            return;

        switch (reason)
        {
            case StopReason.StopAll:
            case StopReason.StopMoveOnly:
                _agent.isStopped = true;
                StopFollowTarget(); // 추적 종료(서버 사인에 의해)
                _agent.ResetPath();
                break;
        }

        // 동기화 루프는 유지(서버에 현재 정지 상태 포지션 계속 보고)
        _lastSentPos = _player.transform.position;
        _lastSentRot = _player.transform.rotation;
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
    public void DeliveryTargetId(int targetId)
    {
        _targetId = targetId;
    }

    public void UpdateTarget(Vector3 targetPos)
    {
        Vector3 dir = targetPos - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f)
            return;

        transform.rotation = Quaternion.LookRotation(dir);

        _player.UpdateTransform();
    }

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

