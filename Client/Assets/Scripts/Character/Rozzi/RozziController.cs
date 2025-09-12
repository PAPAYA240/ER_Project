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
    private NavMeshAgent _agent;

    protected override void Init()
    {
        base.Init();
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = Speed;
        _agent.acceleration = 999;
        _agent.angularSpeed = 720;
        _agent.stoppingDistance = 0.1f;
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
        }
    }

    protected override void GetMouseInput()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        bool raycastHit = Physics.Raycast(ray, out hit, 1000.0f, _mask);

        if (Input.GetMouseButton(1))
        {
            if (raycastHit)
            {
                NavMeshHit navHit;
                if(NavMesh.SamplePosition(hit.point, out navHit, 2.0f, NavMesh.AllAreas))
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

