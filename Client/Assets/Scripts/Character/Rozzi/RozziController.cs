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
using UnityEngine.UIElements;
using static UI_PlayerInterface;
using static UI_SkillBase;
using static UnityEngine.GraphicsBuffer;

public class RozziController : MyPlayerController
{
    // Q
    private Coroutine _coSkillQ = null;
    private bool _canDash = false;
    private bool _isDashing = false;
    private float _dashSpeed = 30.0f;
    private float _dashRange = 4.0f;
    private Vector3 _targetPos;

    // W
    private Coroutine _coSkillW = null;

    // E
    private Coroutine _coSkillE = null;
    private float _animRatio = 0.4f;
    private float _jumpRange = 4.0f;
    private GameObject _skillTarget = null;

    // F
    private float _warpRange = 4.0f;

    protected override void UpdateSkillKeyInput()
    {
        if (IsKeyInput == false && Input.GetKeyDown(KeyCode.Q))
        {
            SetSkillInput(KeyCode.Q);
        }
        else if (IsKeyInput == false && Input.GetKeyDown(KeyCode.W))
        {
            SetSkillInput(KeyCode.W);
        }
        else if (IsKeyInput == false && Input.GetKeyDown(KeyCode.E))
        {
            GameObject target = TryGetAttackableObject();
            if (target == null)
                return;

            Vector3 pos = target.transform.position;

            // TEMP
            // Debug.Log($"Rozzi E : TARGET ON - {pos}");

            if (Vector3.Distance(pos, transform.position) <= _jumpRange)
            {
                _skillTarget = target;
                SetSkillInput(KeyCode.E);
            }
        }
        else if (IsKeyInput == false && Input.GetKeyDown(KeyCode.R))
        {
            SetSkillInput(KeyCode.R);
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {

        }
        else if (Input.GetKeyDown(KeyCode.F))
        {
            Skill_F();
        }
    }

    protected override void GetMouseInput(int mouseButton)
    {
        if (!_canDash && !_isDashing)
            base.GetMouseInput(mouseButton);

        if (Input.GetMouseButton(1) && _canDash)
            StartDash();
    }

    protected override void UpdateMoving()
    {
        if (!_isDashing)
            base.UpdateMoving();
    }

    protected override void UpdateSkill()
    {

    }

    #region Skill
    public override void OnSkillConfirmed(S_Skill skillPacket)
    {
        base.OnSkillConfirmed(skillPacket);

        if ((KeyCode)skillPacket.SkillInfo.KeyCode == KeyCode.Q)
        {
            LookAtMouse();
        }
    }

    protected override void Skill_Q()
    {
        PlayAnimation("SKILL_Q", 0.1f);

        // 스킬이 오브젝트를 맞춰야 Dash 가능
        if (_coSkillQ != null)
            StopCoroutine(_coSkillQ);
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

    private void StartDash()
    {
        Vector3 targetPos = GetTargetPos(_dashRange);

        if (GetReachablePosition(transform.position, targetPos, out NavMeshHit navHit) != Vector3.zero)
        {
            _targetPos = navHit.position;
        }

        _canDash = false;
        if (_coSkillQ != null)
            StopCoroutine(_coSkillQ);
        _coSkillQ = StartCoroutine("CoStartDash");
    }

    IEnumerator CoStartDash()
    {
        PlayAnimation("SKILL_Q_DASH", 0.1f);
        //Debug.Log("Rozzi Dash!!");

        _isDashing = true;
        _agent.ResetPath();
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
        // TEMP
        //if (_targetPos == Vector3.zero)
        //    Debug.Log($"Dash TargetPos : Zero : {_targetPos}");

        transform.position = _targetPos;
        UpdateTransform(true);

        SetMovementState();
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
        if (_skillTarget == null)
        {
            SetMovementState();
            yield break;
        }
            
        _agent.enabled = false;

        Vector3 startPos = transform.position;
        Vector3 midPos = _skillTarget.transform.position;
        Vector3 endPos = Vector3.zero;

        float animLength = GetCurrentAnimClipLength();

        float elapsed = 0.0f;
        while (elapsed < animLength) 
        {           
            Vector3 dir = (midPos - startPos).normalized;
            dir.y = 0f;
            endPos = midPos + dir * _jumpRange;
            endPos = GetReachablePosition(midPos, endPos, out NavMeshHit navHit);
            LookAtTarget(endPos, true);

            float t = elapsed / animLength;

            if(t < _animRatio)
            {
                midPos = _skillTarget.transform.position;

                float midT = t / _animRatio;
                transform.position = Vector3.Lerp(startPos, midPos, midT);
            }
            else
            {
                float endT = (t - (1 - _animRatio)) / _animRatio;
                transform.position = Vector3.Lerp(midPos, endPos, endT);
            }

            UpdateTransform();

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = endPos;
        UpdateTransform();

        ResetTarget();
        _skillTarget = null;
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
        UpdateTransform(true);
    }
    #endregion

    #region Util

    protected override void ResetCharacterState()
    {
        base.ResetCharacterState();

        // Q
        ResetCoroutine(_coSkillQ);
        _canDash = false;
        _isDashing = false;
        _dashSpeed = 30.0f;
        _dashRange = 4.0f;
        _targetPos = Vector3.zero;

        // W
        ResetCoroutine(_coSkillW);

        // E
        ResetCoroutine(_coSkillE);
        _skillTarget = null;
    }
    #endregion
}

