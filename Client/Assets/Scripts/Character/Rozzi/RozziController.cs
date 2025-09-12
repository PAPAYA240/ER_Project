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

public class RozziController : MyPlayerController
{
    protected NavMeshAgent _agent;
    //protected LayerMask _targetLayer = LayerMask.GetMask("")
    protected bool _isTargetOn;
    protected GameObject _targetMonster;

    // TEMP
    protected float _attackRange;

    protected override void Init()
    {
        base.Init();
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = Speed;
        _agent.acceleration = 999;
        _agent.angularSpeed = 720;
        _agent.stoppingDistance = 0.1f;

        _attackRange = 5.0f;
    }

    protected override void UpdateKeyInput()
    {
        base.UpdateKeyInput();
    }

    protected override void UpdateAnimation()
    {
        base.UpdateAnimation();

        if(_agent != null && State != CreatureState.Moving)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
        }
    }

    protected override void UpdateController()
    {
        base.UpdateController();
    }

    protected override void UpdateIdle()
    {
        base.UpdateIdle();
    }

    // 목적지까지 실제로 움직임
    // 목적지까지 도착했으면 Moving 상태 종료 -> Idle
    protected override void UpdateMoving()
    {
        //base.UpdateMoving();

        if (_agent == null)
            return;

        if(!_agent.pathPending)
        {
            // 목적지 도착
            if(_agent.remainingDistance <= _agent.stoppingDistance) 
            {
                State = CreatureState.Idle;
                _moveKeyPressed = false;

                CellPos = transform.position;
                RotInfo = transform.rotation;
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

            if(_isTargetOn)
                LookAtTarget();
        }
    }

    protected void LookAtTarget()
    {
        Vector3 lookDir = (_targetMonster.transform.position - transform.position).normalized;
        lookDir.y = 0f;

        if(lookDir != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 10f);
        }
    }

    // 마우스 우클릭이 눌렸을 경우 유효한 곳이 클릭 되었다면 해당 위치를 목적지로 설정
    // 몬스터 클릭 시 평타 사거리만큼 떨어진 곳으로 설정
    // Moving 상태로 변경
    protected override void GetMouseInput()
    {
        if (Input.GetMouseButton(1))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            bool raycastHit = Physics.Raycast(ray, out hit, 1000.0f);
            
            if (raycastHit)
            {
                GameObject targetObject = hit.collider.gameObject;
                Vector3 targetPos;

                // 마우스와 충돌한 물체가
                // 몬스터인 경우
                if (targetObject.layer != LayerMask.NameToLayer("Map"))
                {
                    _isTargetOn = true;
                    _targetMonster = targetObject;

                    Vector3 monsterPos = targetObject.transform.position;
                    Vector3 dir = (monsterPos - transform.position).normalized;

                    float distance = Vector3.Distance(transform.position, monsterPos);

                    // TODO : 실제 사거리 가져와야함!
                    // 이미 사거리 안이라면 제자리
                    if (distance <= _attackRange)
                        targetPos = transform.position;
                    else
                        targetPos = monsterPos - dir * _attackRange;
                }
                // 맵일 경우
                else
                {
                    _isTargetOn = false;
                    _targetMonster = null;

                    targetPos = hit.point;
                }

                if (NavMesh.SamplePosition(targetPos, out NavMeshHit navHit, 2.0f, NavMesh.AllAreas))
                {
                    _agent.SetDestination(navHit.position);
                    State = CreatureState.Moving;

                    _moveKeyPressed = true;
                }
            }
        }
    }

    protected override void UpdateDead()
    {
    }
}

