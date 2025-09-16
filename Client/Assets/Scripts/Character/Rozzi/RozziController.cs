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

public class RozziController : MyPlayerController
{
    private Coroutine _coSkillQ = null;
    private bool _canDash = false;
    private float _dashRange = 3.0f;
    
    private bool _isDashing = false;

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
            _isUseSkill = true;
            _keyCode = KeyCode.E;
        }
        else if (IsKeyInput == false && Input.GetKeyDown(KeyCode.R))
        {
            _isUseSkill = true;
            _keyCode = KeyCode.R;
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {

        }
    }

    protected override void GetMouseInput()
    {
        if(!_canDash)
            base.GetMouseInput();

        if(Input.GetMouseButtonDown(1) && _canDash)
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            bool raycastHit = Physics.Raycast(ray, out hit, 1000.0f);

            Vector3 targetPos;
            Vector3 dir = (hit.point - transform.position).normalized;

            targetPos = transform.position + dir * _dashRange;

            // 최종 목적지 설정
            if (NavMesh.SamplePosition(targetPos, out NavMeshHit navHit, 2.0f, NavMesh.AllAreas))
            {
                _agent.SetDestination(navHit.position);
                State = CreatureState.Moving;

                _moveKeyPressed = true;
            }

            _agent.speed = 50.0f;

            _canDash = false;
            _isDashing = true;
            StopCoroutine(_coSkillQ);
        }
    }

    protected override void UpdateMoving()
    {
        // 목적지까지 실제로 움직임
        // 목적지까지 도착했으면 Moving 상태 종료 -> Idle

        if (_agent == null)
            return;

        if (!_agent.pathPending)
        {
            // 목적지 도착
            if (_agent.remainingDistance <= _agent.stoppingDistance)
            {
                State = CreatureState.Idle;
                _moveKeyPressed = false;

                CellPos = transform.position;
                RotInfo = transform.rotation;

                if (_isDashing)
                {
                    _agent.speed = Speed;
                    _isDashing = false;
                }

                CheckUpdatedFlag();
            }
            // 이동 중
            else
            {
                State = CreatureState.Moving;
                CellPos = transform.position;
                RotInfo = transform.rotation;
                CheckUpdatedFlag();
            }

            if (_isTargetOn)
                LookAtTarget();
        }
    }

    protected override void UpdateSkill()
    {

    }

    #region Skill
    protected override void Skill_Q()
    {
        base.Skill_Q();
        _coSkillQ = StartCoroutine("CoCheckDash");
    }

    //protected override void Skill_W()
    //{

    //}

    //protected override void Skill_E()
    //{

    //}

    //protected override void Skill_R()
    //{

    //}
    #endregion

    #region Skill : Q
    IEnumerator CoCheckDash()
    {
        _canDash = true;
        yield return new WaitForSeconds(3.0f);
        _canDash = false;
    }

    #endregion
}

