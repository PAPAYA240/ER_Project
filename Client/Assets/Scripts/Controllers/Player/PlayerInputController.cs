using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Assertions.Must;
using static UnityEngine.GraphicsBuffer;

public class PlayerInputController : MonoBehaviour
{
    private MyPlayerController _player;    
    private NavMeshAgent _agent;         

    [SerializeField] float _attackRange = 3.0f;  
    [SerializeField] float _stopBuffer = 0.1f;  

    private void Awake()
    {
        _player = GetComponentInChildren<MyPlayerController>();
        _agent = GetComponentInChildren<NavMeshAgent>();
    }

    public C_Move GetMoveCommand()
    {
        if (Input.GetMouseButton(1))
        {
            GameObject target = GetAttackableUnderCursor();
            if (target == null)
            {
                if (TryGetGroundDestination(out Vector3 final))
                {
                    return new C_Move
                    {
                        IsTargetOn = false,
                        TargetPosition = final 
                    };
                }
            }
            else
            {
                if (TryGetTargetDestination(target, out Vector3 final, out int targetId))
                {
                    return new C_Move
                    {
                        IsTargetOn = true,
                        TargetId = targetId,
                        TargetPosition = final 
                    };
                }
            }
        }
        return null;
    }

    private static readonly KeyCode[] _skillKeys =
    {
        KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.F
    };

    public C_Skill GetSkillCommand()
    {
        // 배열 순서대로 키다운 검사 -> 처음 눌린 키에 대해 바로 생성/리턴
        for (int i = 0; i < _skillKeys.Length; i++)
        {
            var key = _skillKeys[i];
            if (!Input.GetKeyDown(key))
                continue;

            var mousePos = GetMouseWorldPosition();
            return new C_Skill
            {
                SkillInfo = new SkillInfo { KeyCode = (int)key },
                MousePosX = mousePos.x,
                MousePosZ = mousePos.z
            };
        }

        return null;
    }

    //public C_Attack GetAttackCommand()
    //{
    //    if (Input.GetKeyDown(KeyCode.A))
    //    {
    //        return new C_Attack { TargetId = 123 }; // TODO: 실제 타겟 선택
    //    }
    //    return null;
    //}

    //public C_Rest GetRestCommand()
    //{
    //    if (Input.GetKeyDown(KeyCode.X))
    //    {
    //        return new C_Rest();
    //    }
    //    return null;
    //}

    #region Util
    private Vector3 GetMouseWorldPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
            return hit.point;
        return Vector3.zero;
    }

    private GameObject GetAttackableUnderCursor(float radius = 0.1f)
    {
        GameObject gameObject = null;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        int monsterMask = 1 << LayerMask.NameToLayer("Monster");
        int playerMask = 1 << LayerMask.NameToLayer("Fog");
        if (Physics.SphereCast(ray, radius, out RaycastHit sphereHit, 1000.0f, monsterMask | playerMask))
        {
            GameObject hitObject = sphereHit.collider.gameObject;
            CreatureController cc = hitObject.GetComponent<CreatureController>();
            if (IsAttackable(hitObject))    // TEMP : _player.IsAttackable
            {
                gameObject = hitObject;
            }
        }

        return gameObject;
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

        CreatureController cc = targetObject.GetComponent<CreatureController>();
        if (cc == null)
            return false;

        // 나 자신일 때
        if (cc.Id == _player.Id)
            return false;

        // 같은 팀일 때
        if (cc.ObjectType == Define.Object.OtherPlayer && cc.ObjInfo.Player.Team == _player.ObjInfo.Player.Team)
            return false;

        // 대상이 죽었을 때 || 무적 상태일 때 || 시야 밖일 때(부시) 등등
        if (cc.State == CreatureState.Dead)
            return false;

        return true;
    }
    #endregion
}

