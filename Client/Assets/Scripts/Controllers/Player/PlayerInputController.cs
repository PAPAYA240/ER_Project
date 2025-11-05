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

    [SerializeField] float _attackRange = 3.0f;  
    [SerializeField] float _stopBuffer = 1.5f;

    private Vector3 _lastCmdDest;
    private int _lastCmdTargetId;
    private bool _lastCmdIsTarget;
    [SerializeField] float _destEps = 0.15f; // 15cm 이상 움직일 때만 새 명령

    private GameObject _target;

    [SerializeField] private LayerMask _groundMask;
    [SerializeField] private LayerMask _monsterMask;
    [SerializeField] private LayerMask _playerMask;

    private bool isRest = false;

    private void Awake()
    {
        _player = GetComponentInChildren<MyPlayerController>();
        _agent = GetComponentInChildren<NavMeshAgent>();
        _skill = GetComponentInChildren<PlayerSkillController>();

        _groundMask = 1 << LayerMask.NameToLayer("Map");
        _monsterMask = 1 << LayerMask.NameToLayer("Monster");
        _playerMask = 1 << LayerMask.NameToLayer("Player");    // TEMP
    }

    // 우클릭 유지 중 이동 의도(타겟 이동 or 땅 이동)
    public C_SetMoveTarget GetSetMoveTarget()
    {
        if (_player.State == CreatureState.Idle || _player.State == CreatureState.Moving || _player.State == CreatureState.Attack || _player.State == CreatureState.Skill)
        {
            if (!Input.GetMouseButton(1))
                return null;

            GameObject target = GetAttackableUnderCursor();
            if (target == null)
            {
                // 땅 이동
                if (!TryGetGroundDestination(out Vector3 final))
                    return null;

                // 중복 억제: 연속 같은 목적지면 전송 생략
                if (!_lastCmdIsTarget &&
                    (final - _lastCmdDest).sqrMagnitude < _destEps * _destEps)
                    return null;

                _lastCmdIsTarget = false;
                _lastCmdDest = final;
                _lastCmdTargetId = 0;

                return new C_SetMoveTarget
                {
                    IsGround = true,
                    TargetPos = new PositionInfo { PosX = final.x, PosY = final.y, PosZ = final.z }
                };
            }
            else
            {
                // 타겟팅 이동
                if (!TryGetTargetDestination(target, out Vector3 final, out int id))
                    return null;

                // 중복 억제: 같은 타겟이면 전송 생략
                if (_lastCmdIsTarget && id == _lastCmdTargetId)
                    return null;

                _lastCmdIsTarget = true;
                _lastCmdTargetId = id;
                _lastCmdDest = final; // (정보 유지용)

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
        if (_player.State == CreatureState.Idle || _player.State == CreatureState.Moving || _player.State == CreatureState.Attack)
        {
            if (!Input.GetKeyDown(KeyCode.C) /*|| !Input.GetMouseButtonDown(1)*/)
                return null;

            int id = GetAttackableUnderCursorID();
            if (id == 0)
                return null;

            _target = Managers.Object.FindById(id);
            if (_target == null)
                return null;

            return new C_Attack { TargetId = id };
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

    private static readonly KeyCode[] _skillKeys =
    {
        KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.D, KeyCode.F
    };

    public C_SkillInput GetSkillCommand()
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
    private Vector3 GetAttackStopPosition(Vector3 from, Vector3 target)
    {
        Vector3 dir = target - from;
        dir.y = 0f;
        float dist = dir.magnitude;
        if (dist <= Mathf.Epsilon)
            return target;
        dir /= dist;

        float stop = Mathf.Max(0.05f, _attackRange - _stopBuffer); 
        return target - dir * stop;
    }

    // 경로가 부분 경로면 마지막 코너를 반환
    private Vector3 CalculateFinalDestination(Vector3 from, Vector3 desired)
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

