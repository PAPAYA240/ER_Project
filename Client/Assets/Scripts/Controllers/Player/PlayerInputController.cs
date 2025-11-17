using Data;
using Google.Protobuf.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class PlayerInputController : MonoBehaviour
{
    protected MyPlayerController _player;    
    protected PlayerSkillController _skill;
    private NavMeshAgent _agent;

    [SerializeField] float _stopBuffer = 0.1f;

    private GameObject _target;

    [SerializeField] private LayerMask _groundMask;
    [SerializeField] private LayerMask _monsterMask;
    [SerializeField] private LayerMask _playerMask;
    [SerializeField] private LayerMask _beaconMask;

    private bool isRest = false;

    // 커서가 올려져 있는 현재 타겟
    private GameObject _hoverTarget;

    // 공격 입력을 얼마나 자주 서버로 보낼지 제한 (스팸 방지)
    private float _nextAutoAttackSendTime;
    [SerializeField] private float _attackInputInterval = 0.08f; // 0.08초마다 1번(초당 약 12번)

    private void Awake()
    {
        _player = GetComponentInChildren<MyPlayerController>();
        _agent = GetComponentInChildren<NavMeshAgent>();
        _skill = GetComponentInChildren<PlayerSkillController>();

        _groundMask = 1 << LayerMask.NameToLayer("Map");
        _monsterMask = 1 << LayerMask.NameToLayer("Monster");
        _playerMask = 1 << LayerMask.NameToLayer("Player");
        _beaconMask = 1 << LayerMask.NameToLayer("Beacon");
    }

    // 우클릭 유지 중 이동 의도(타겟 이동 or 땅 이동)
    public virtual C_SetMoveTarget GetSetMoveTarget()
    {
        if (_player.State == CreatureState.Idle 
            || _player.State == CreatureState.Moving 
            || _player.State == CreatureState.Attack 
            || _player.State == CreatureState.Skill 
            || _player.State == CreatureState.Operate)
        {
            if (!Input.GetMouseButton(1))
                return null;

            GameObject target = GetAttackableUnderCursor();
            if (target == null)
            {
                // 땅 이동
                if (!TryGetGroundDestination(out Vector3 final))
                    return null;

                return new C_SetMoveTarget
                {
                    IsGround = true,
                    TargetPos = new PositionInfo { PosX = final.x, PosY = final.y, PosZ = final.z }
                };
            }
            else
            {
                //// 타겟팅 이동
                //if (!TryGetTargetDestination(target, out Vector3 final, out int id))
                //    return null;

                //return new C_SetMoveTarget
                //{
                //    IsGround = false,
                //    TargetId = id,
                //    TargetPos = final,
                //};

                // ★ 타겟 기억
                _target = target;

                // === 사거리 안이면 이동 명령을 보내지 않음 ===
                Vector3 myPos = _player.transform.position;
                Vector3 targetPos = _target.transform.position;

                Vector2 myXZ = new Vector2(myPos.x, myPos.z);
                Vector2 targetXZ = new Vector2(targetPos.x, targetPos.z);
                float dist = Vector2.Distance(myXZ, targetXZ);

                float effectiveRange = Mathf.Max(0.05f, _player.AttackRange - _stopBuffer);

                // 이미 평타 사거리 안이다 → 이동 패킷 안 보냄
                if (dist <= effectiveRange)
                    return null;

                // === 사거리 밖일 때만 추적 이동 패킷 ===
                if (!TryGetTargetDestination(target, out Vector3 final, out int id))
                    return null;

                return new C_SetMoveTarget
                {
                    IsGround = false,
                    TargetId = id,
                    TargetPos = final,
                };
            }
        }
        else if (_player.State == CreatureState.Rest)
        {
            if(!_agent.enabled)
                _agent.enabled = true;
            _agent.isStopped = true;
            return null;
        }
        else
        {
            return null;
        }
    }

    // 우클릭 "타겟 공격" (클릭 순간 1회)
    public C_Attack GetAttackCommand()
    {
        //if (_player.State == CreatureState.Idle || _player.State == CreatureState.Moving || _player.State == CreatureState.Attack)
        //{
        //    if (/*!Input.GetKeyDown(KeyCode.C) ||*/ !Input.GetMouseButtonDown(1))
        //        return null;

        //    int id = GetAttackableUnderCursorID();
        //    if (id == 0)
        //        return null;

        //    _target = Managers.Object.FindById(id);
        //    if (_target == null)
        //        return null;

        //    return new C_Attack { TargetId = id };
        //}

        //return null;

        // 공격 가능한 상태만 처리
        if (!(_player.State == CreatureState.Idle
            || _player.State == CreatureState.Moving
            || _player.State == CreatureState.Attack))
            return null;

        // 우클릭이 아예 안 눌려 있으면 상태 리셋
        if (!Input.GetMouseButton(1))
        {
            _hoverTarget = null;
            _nextAutoAttackSendTime = 0f;
            return null;
        }

        // 커서 아래 공격 가능한 대상 찾기
        GameObject target = GetAttackableUnderCursor();
        _hoverTarget = target;

        if (_hoverTarget == null)
            return null;

        var cc = _hoverTarget.GetComponent<CreatureController>();
        if (cc == null)
            return null;

        // ===== 거리(사거리) 체크 =====
        Vector3 myPos = _player.transform.position;
        Vector3 targetPos = cc.transform.position;

        Vector2 myXZ = new Vector2(myPos.x, myPos.z);
        Vector2 targetXZ = new Vector2(targetPos.x, targetPos.z);
        float dist = Vector2.Distance(myXZ, targetXZ);

        float effectiveRange = Mathf.Max(0.05f, _player.AttackRange - _stopBuffer);

        if (dist > effectiveRange)
            return null;

        // ===== 실제로 공격 패킷을 보낼지 결정 =====
        bool explicitClick = Input.GetMouseButtonDown(1); // 딱 누른 순간
        bool autoRepeat = Input.GetMouseButton(1) && Time.time >= _nextAutoAttackSendTime; // 홀드 중 자동 반복

        if (!explicitClick && !autoRepeat)
            return null;

        // 다음 자동 공격 입력 시간 갱신 (스팸 방지용)
        _nextAutoAttackSendTime = Time.time + _attackInputInterval;

        // 공격 패킷 생성
        return new C_Attack { TargetId = cc.Id };
    }

    public C_Operate GetOperateCommand()
    {
        if(_player.State == CreatureState.Idle || _player.State == CreatureState.Moving || _player.State == CreatureState.Attack)
        {
            if (!Input.GetMouseButtonDown(1))
                return null;

            GameObject beacon = GetBeaconUnderCursor();
            if (null == beacon)
                return null;

            C_Operate operatePkt = new C_Operate();
            operatePkt.BeaconName = beacon.name;

            Vector3 playerPos = _player.transform.position;
            Vector3 beaconPos = beacon.transform.position;

            Vector3 dir = (beaconPos - playerPos).normalized;
            float distance = Vector3.Distance(playerPos, beaconPos);

            Vector3 bestPos = playerPos;
            bool found = false;

            // 일정 간격으로 앞으로 이동하면서 네비메쉬 위 지점 탐색
            for (float d = 0.5f; d <= distance; d += 0.5f)
            {
                Vector3 checkPos = playerPos + dir * d;
                if (NavMesh.SamplePosition(checkPos, out NavMeshHit hit, 0.4f, NavMesh.AllAreas))
                {
                    bestPos = hit.position;
                    found = true;
                }
            }

            // 혹시 플레이어-비콘 사이에 네비 지점이 없으면 비콘 근처라도 시도
            if (!found && NavMesh.SamplePosition(beaconPos, out NavMeshHit fallback, 3.0f, NavMesh.AllAreas))
            {
                bestPos = fallback.position;
            }

            operatePkt.PosX = bestPos.x;
            operatePkt.PosZ = bestPos.z;

            return operatePkt;
        }

        return null;
    }

    // S키 : 공격, 이동 중지 -> Idle 상태 벗어나면 다시 자동 공격
    // H키 : 이동 중지
    public C_Stop GetStopCommand()
    {
        if (Input.GetKeyDown(KeyCode.S))
            return new C_Stop { Reason = StopReason.StopAll };
        if (Input.GetKeyDown(KeyCode.H))
            return new C_Stop { Reason = StopReason.StopMoveOnly };
        return null;
    }

    protected static readonly KeyCode[] _skillKeys =
    {
        KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.D, KeyCode.F
    };

    public virtual C_SkillInput GetSkillCommand()
    {
        // 배열 순서대로 키다운 검사 -> 처음 눌린 키에 대해 바로 생성/리턴
        for (int i = 0; i < _skillKeys.Length; i++)
        {
            var key = _skillKeys[i];
            if (!Input.GetKeyDown(key))
                continue;

            if (IsCharge(key))
            {
                ChargeSkill(key);
                return null;
            }

            return _skill.TryCast((int)key, GetAttackableUnderCursorID(), GetMouseWorldPosition());
        }
        return null;
    }

    private bool IsCharge(KeyCode key)
    {
        SkillData skillData = DataManager.SkillDict[_player.ObjInfo.Player.CharType][key];
        if (skillData == null)
            return false;

        if (Enum.TryParse(skillData.skillType, out SkillInputType skillType))
        {
            if (skillType == SkillInputType.Charge)
                return true;
        }
        return false;
    }

    public C_Rest GetRestCommand()
    {
        if (isRest == false)
        {
            if (Input.GetKeyDown(KeyCode.X))
            {
                isRest = true;
                return new C_Rest() { IsRest = true };
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.X) || Input.GetMouseButtonDown(1))
            {
                isRest = false;
                return new C_Rest() { IsRest = false };
            }
        }

        return null;
    }

    // temp 임시 커맨드 나중에 삭제
    public C_Death GetDieCommand()
    {
        if (Input.GetKeyDown(KeyCode.Z))
            return new C_Death() { IsDeath = true };

        return null;
    }

    public KeyCode GetSkillLevelUpCommand()
    {
        if (Input.GetKey(KeyCode.LeftControl))
        {
            if (Input.GetKeyDown(KeyCode.Q))        { return KeyCode.Q; }       
            else if (Input.GetKeyDown(KeyCode.W))   { return KeyCode.W; } 
            else if (Input.GetKeyDown(KeyCode.E))   { return KeyCode.E; } 
            else if (Input.GetKeyDown(KeyCode.R))   { return KeyCode.R; }
            else if (Input.GetKeyDown(KeyCode.T)) { return KeyCode.T; }
        }

        return KeyCode.None;
    }

    public C_KeyInputForTest Get_KeyInputForTestCommand()
    {
        if (Input.GetKeyDown(KeyCode.L))
            return new C_KeyInputForTest() { KeyCode = (int)KeyCode.L };

        return null;
    }

    #region Charge
    protected virtual void ChargeSkill(KeyCode key)
    {
    }

    #endregion

    #region Util
    protected Vector3 GetMouseWorldPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
            return hit.point;
        return Vector3.zero;
    }

    private GameObject GetAttackableUnderCursor(float radius = 0.1f)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.SphereCast(ray, radius, out RaycastHit hit, 1000f, _monsterMask | _playerMask))
        {
            var cc = hit.collider.GetComponentInChildren<CreatureController>();
            if (cc != null && IsAttackable(hit.collider.gameObject))
                return cc.gameObject;
        }

        return null;
    }

    protected int GetAttackableUnderCursorID(float radius = 0.1f)
    {
        GameObject target = GetAttackableUnderCursor();
        if (target == null)
            return 0;

        var cc = target.GetComponentInChildren<CreatureController>();
        if (cc == null)
            return 0;

        return cc.Id;
    }

    GameObject GetBeaconUnderCursor(float radius = 0.1f)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if(Physics.SphereCast(ray, radius, out RaycastHit hit, 1000f, _beaconMask))
        {
            return hit.collider.gameObject;
        }

        return null;
    }

    // 지형 클릭 시
    private bool TryGetGroundDestination(out Vector3 final)
    {
        final = default;

        int mapMask = 1 << LayerMask.NameToLayer("Map");
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out var hit, 1000f, mapMask))
            return false;

        if (!NavMesh.SamplePosition(hit.point, out var navHit, 2f, NavMesh.AllAreas))
            return false;

        Vector3 desired = navHit.position;
        final = CalculateFinalDestination(_player.transform.position, desired);
        return true;
    }

    // 타겟 클릭 시
    private bool TryGetTargetDestination(GameObject targetGo, out Vector3 final, out int targetId)
    {
        final = default;
        targetId = 0;

        var cc = targetGo.GetComponentInChildren<CreatureController>();
        if (cc == null)
            return false;
        targetId = cc.Id;

        Vector3 targetPos = targetGo.transform.position;
        Vector3 desiredStop = GetAttackStopPosition(_player.transform.position, targetPos);
        final = CalculateFinalDestination(_player.transform.position, desiredStop);
        return true;
    }

    // 사거리-타겟 지점 계산
    protected virtual Vector3 GetAttackStopPosition(Vector3 from, Vector3 target)
    {
        Vector3 dir = target - from;
        dir.y = 0f;
        float dist = dir.magnitude;
        if (dist <= Mathf.Epsilon)
            return target;
        dir /= dist;

        float stop = Mathf.Max(0.05f, _player.AttackRange - _stopBuffer); 
        return target - dir * stop;
    }

    // 경로가 부분 경로면 마지막 코너를 반환
    protected virtual Vector3 CalculateFinalDestination(Vector3 from, Vector3 desired)
    {
        if (!NavMesh.SamplePosition(from, out var fromHit, 2f, NavMesh.AllAreas))
            fromHit.position = from;
        if (!NavMesh.SamplePosition(desired, out var toHit, 2f, NavMesh.AllAreas))
            toHit.position = desired;

        Vector3 start = fromHit.position;
        Vector3 end = toHit.position;

        var path = new NavMeshPath();
        if (!NavMesh.CalculatePath(start, end, NavMesh.AllAreas, path) || path.corners.Length == 0)
        {
            return end;
        }

        return path.corners[path.corners.Length - 1];
    }

    private bool IsAttackable(GameObject targetObject)
    {
        if (targetObject == null)
            return false;

        CreatureController cc = targetObject.GetComponentInChildren<CreatureController>();
        if (cc == null)
            return false;

        if (cc.Untargetable)
            return false;

        // 나 자신일 때
        if (cc.Id == _player.Id)
            return false;

        // 같은 팀일 때
        if (cc.ObjInfo.Player != null && cc.ObjInfo.Player.Team == _player.ObjInfo.Player.Team)
            return false;

        // 대상이 죽었을 때 || 무적 상태일 때 || 시야 밖일 때(부시) 등등
        if (cc.State == CreatureState.Dead)
            return false;

        return true;
    }
    #endregion

}

