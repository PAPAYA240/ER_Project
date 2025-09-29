using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Assertions.Must;
using static UnityEditor.PlayerSettings;

public class PlayerInputController : MonoBehaviour
{
    private MyPlayerController _player;     // TEMP
    private NavMeshAgent _agent;            // TEMP

    public void SetPlayer(MyPlayerController player)
    {
        _player = player;
    }

    public void SetAgent(NavMeshAgent agent)
    {
        _agent = agent;
    }

    public C_Move GetMoveCommand()
    {
        if (Input.GetMouseButton(1))
        {
            Vector3 dest = GetMouseWorldPosition();

            if (NavMesh.SamplePosition(dest, out NavMeshHit navHit, 2.0f, NavMesh.AllAreas))
            {
                return new C_Move
                {
                    IsTargetOn = false,
                    TargetPosition = navHit.position,
                };
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

    private Vector3 GetMouseWorldPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
            return hit.point;
        return Vector3.zero;
    }
}

