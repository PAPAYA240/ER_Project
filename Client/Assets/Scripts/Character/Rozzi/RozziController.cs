using Google.Protobuf.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Playables;
using static UI_PlayerInterface;
using static UI_SkillBase;
using static UnityEngine.GraphicsBuffer;

public class RozziController : MyPlayerController
{
    private Coroutine _coSkillQ = null;
    private bool _canDash = false;
    private bool _isDashing = false;
    private float _dashSpeed = 30.0f;
    private float _dashRange = 4.0f;
    private Vector3 _targetPos;

    private Coroutine _coSkillW = null;

    private Coroutine _coSkillE = null;
    private float _jumpRange = 4.0f;

    private float _warpRange = 4.0f;

    protected override void UpdateSkillKeyInput()
    {
        if (IsKeyInput == false && Input.GetKeyDown(KeyCode.Q))
        {
            _isUseSkill = true;
            _keyCode = KeyCode.Q;
        }
        else if (IsKeyInput == false && Input.GetKeyDown(KeyCode.W))
        {
            _isUseSkill = true;
            _keyCode = KeyCode.W;
        }
        else if (IsKeyInput == false && Input.GetKeyDown(KeyCode.E))
        {
            if(FindMonster() != Vector3.zero)
            {
                _isUseSkill = true;
                _keyCode = KeyCode.E;
            }
        }
        else if (IsKeyInput == false && Input.GetKeyDown(KeyCode.R))
        {
            _isUseSkill = true;
            _keyCode = KeyCode.R;
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {

        }
        else if (Input.GetKeyDown(KeyCode.F))
        {
            Skill_F();
        }
    }

    protected override void GetMouseInput()
    {
        if(!_canDash && !_isDashing)
            base.GetMouseInput();

        if(Input.GetMouseButton(1) && _canDash)
            StartDash();
    }

    protected override void UpdateMoving()
    {
        if(!_isDashing)
            base.UpdateMoving();
    }

    protected override void UpdateSkill()
    {

    }

    #region Skill
    protected override void Skill_Q()
    {
        PlayAnimation("SKILL_Q", 0.1f);

        // 스킬이 오브젝트를 맞춰야 Dash 가능
        _coSkillQ = StartCoroutine("CoCheckDash");
    }

    protected override void Skill_W()
    {
        PlayAnimation("SKILL_W", 0.1f);

        // 스킬 시전 시 움직일 수 있음 (Speed *= 1.2f)
        _coSkillW = StartCoroutine("CoStartW");
    }

    protected override void Skill_E()
    {
        PlayAnimation("SKILL_E", 0.1f);

        // 사거리 내 타겟팅 된 몬스터가 있어야 스킬 시전 가능
        _coSkillE = StartCoroutine("CoStartE");
    }

    protected override void Skill_R()
    {
        PlayAnimation("SKILL_R", 0.1f);
    }
    #endregion

    #region Skill : Q
    IEnumerator CoCheckDash()
    {
        _canDash = true;
        yield return new WaitForSeconds(3.0f);
        _canDash = false;
    }

    IEnumerator CoStartDash()
    {
        PlayAnimation("SKILL_Q_DASH", 0.0f);

        _isDashing = true;
        _agent.enabled = false;
        State = CreatureState.Skill;

        LookAtTarget(_targetPos, true);
   
        while (Vector3.Distance(transform.position, _targetPos) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, _targetPos, _dashSpeed * Time.deltaTime);
            UpdateTransform();
            yield return null;
        }

        _isDashing = false;
        _agent.enabled = true;

        _agent.Warp(_targetPos);
        transform.position = _targetPos;
        UpdateTransform();

        SetMovementState();
    }

    private void StartDash()
    {
        Vector3 targetPos = GetTargetPos(_dashRange);

        if (GetReachablePosition(transform.position, targetPos, out NavMeshHit navHit) != Vector3.zero)
        {
            _targetPos = navHit.position;
            _agent.SetDestination(_targetPos);
            State = CreatureState.Moving;

            _moveKeyPressed = true;
        }

        _canDash = false;
        StopCoroutine(_coSkillQ);
        StartCoroutine("CoStartDash");
    }
    #endregion

    #region Skill : W
    IEnumerator CoStartW()
    {
        float startTimte = Time.time;
        float animLength = GetCurrentAnimClipLength();

        _agent.speed = Speed * 1.2f;

        while (true)
        {
            // 마우스 우클릭 시 -> 목적지 설정
            if (Input.GetMouseButton(1))
            {
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                bool raycastHit = Physics.Raycast(ray, out hit, 1000.0f);

                Vector3 targetPos;

                targetPos = hit.point;

                if (NavMesh.SamplePosition(targetPos, out NavMeshHit navHit, 2.0f, NavMesh.AllAreas))
                {
                    _agent.SetDestination(navHit.position);

                    _moveKeyPressed = true;
                }
            }

            if (_agent == null)
                break;

            // 이동
            if (!_agent.pathPending)
            {
                // 목적지 도착
                if (_agent.remainingDistance <= _agent.stoppingDistance)
                {
                    _moveKeyPressed = false;
                }

                UpdateTransform();
            }

            if (Time.time - startTimte >= animLength)
            {
                _agent.speed = Speed;
                SetMovementState();
                break;
            }

            yield return null;
        }
    }
    #endregion

    #region Skill : E
    IEnumerator CoStartE()
    {
        if (_targetMonster == null)
        {
            SetMovementState();
            yield break;
        }
            
        _agent.enabled = false;

        Vector3 startPos = transform.position;
        Vector3 midPos = _targetMonster.transform.position;
        Vector3 dir = (midPos - startPos).normalized;
        Vector3 endPos = midPos + dir * _jumpRange;
        endPos = GetReachablePosition(midPos, endPos, out NavMeshHit navHit);
        LookAtTarget(endPos, true);

        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        float animLength = stateInfo.length;

        float elapsed = 0.0f;
        while (elapsed < animLength) 
        {
            float t = elapsed / animLength;

            if(t < 0.5f)
            {
                float midT = t / 0.5f;
                transform.position = Vector3.Lerp(startPos, midPos, midT);
            }
            else
            {
                float endT = (t - 0.5f) / 0.5f;
                transform.position = Vector3.Lerp(midPos, endPos, endT);
            }

            UpdateTransform();

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = endPos;
        UpdateTransform();

        _agent.enabled = true;
    }
    #endregion

    #region Skill : F
    private void Skill_F()
    {
        Vector3 targetPos = GetTargetPos(_warpRange, false);

        if (NavMesh.SamplePosition(targetPos, out NavMeshHit navHit, 1.0f, NavMesh.AllAreas))
        {
            targetPos = navHit.position;
        }

        LookAtTarget(targetPos, true);
        transform.position = targetPos;
        _agent.Warp(targetPos);
        UpdateTransform();
    }
    #endregion
}

