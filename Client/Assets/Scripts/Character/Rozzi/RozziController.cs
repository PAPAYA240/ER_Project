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
    protected NavMeshAgent _agent;
    //protected LayerMask _targetLayer = LayerMask.GetMask("")
    protected bool _isTargetOn;
    protected GameObject _targetMonster;

    // State : Rest
    protected bool _isResting = false;
    protected Coroutine _coRest;

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

        _attackRange = 3.0f;
    }

    protected override void UpdateKeyInput()
    {
        if (Input.GetKey(KeyCode.LeftControl))
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                _playerInterface.SpecificSkillLevelUp(GameObjects.QSkill);
            }
            else if (Input.GetKeyDown(KeyCode.W))
            {
                _playerInterface.SpecificSkillLevelUp(GameObjects.WSkill);
            }
            else if (Input.GetKeyDown(KeyCode.E))
            {
                _playerInterface.SpecificSkillLevelUp(GameObjects.ESkill);
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                _playerInterface.SpecificSkillLevelUp(GameObjects.RSkill);
            }
        }
        else
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

        // 처음 X를 눌렀고 Idle이나 Moving 상태였을 때 -> Rest 상태로 변경
        // 다시 X를 누르면 -> 휴식 종료
        if (Input.GetKeyDown(KeyCode.X))
        {
            if(!_isResting && (State == CreatureState.Idle || State == CreatureState.Moving))
            {
                State = CreatureState.Rest;
                _isResting = true;
            }
            else if(_isResting)
            {
                ExitRest();              
            }
        }
    }

    // 상태가 전환되면 한 번만 호출됨
    protected override void UpdateAnimation()
    {
        base.UpdateAnimation();

        if(State == CreatureState.Rest)
        {
            PlayAnimation("REST_START", 0.1f);
        }

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

    protected override void UpdateDead()
    {
    }

    // TODO : 쉬는 동안 자원 회복
    protected override void UpdateRest()
    {
        
    }

    // 타겟을 바라보도록 방향 조정
    protected void LookAtTarget()
    {
        Vector3 lookDir = (_targetMonster.transform.position - transform.position).normalized;
        lookDir.y = 0f;

        if (lookDir != Vector3.zero)
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
                if (targetObject.layer == LayerMask.NameToLayer("Monster"))
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

    // 휴식 종료 애니메이션 재생
    // 종료 시점을 
    protected void ExitRest()
    {
        PlayAnimation("REST_END", 0.1f);
        _coRest = StartCoroutine(CoRestEnd());
    }

    // 애니메이션 종료 시점을 체크해서 Idle or Moving 상태로 전환
    IEnumerator CoRestEnd()
    {
        yield return new WaitForSeconds(0.1f);

        float elapsed = 0f;

        AnimatorClipInfo[] clipInfos = _animator.GetCurrentAnimatorClipInfo(0);
        if (clipInfos.Length > 0)
        {
            float length = clipInfos[0].clip.length;
            while (elapsed < length - 0.1f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        _isResting = false;

        if (_moveKeyPressed)
            State = CreatureState.Moving;
        else
            State = CreatureState.Idle;
    }
}

