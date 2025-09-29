using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.GraphicsBuffer;

public class PlayerViewController : MonoBehaviour
{
    private NavMeshAgent _agent;
    private Animator _animator;

    private void Awake()
    {
        _agent = GetComponentInChildren<NavMeshAgent>();
        _animator = GetComponentInChildren<Animator>();
    }

    //public void OnIdle(S_Idle packet)
    //{

    //}

    public void OnMove(S_Move packet)
    {
        Vector3 targetPos = new Vector3()
        {
            x = packet.PosInfo.PosX,
            y = packet.PosInfo.PosY,
            z = packet.PosInfo.PosZ
        };

        if (NavMesh.SamplePosition(targetPos, out NavMeshHit navHit, 2.0f, NavMesh.AllAreas))
        {
            _agent.SetDestination(navHit.position);
        }
    }

    public void OnSkill(S_Skill packet)
    {
        if(packet.CanUse)
        {
            //_animator.SetTrigger("Skill" + (KeyCode)packet.SkillInfo.KeyCode);
        }
    }

    public void OnAnim(S_Anim packet)
    {
        //_animator.SetTrigger(packet.AnimInfo.AnimName);
    }

    public void OnHpChanged(S_ChangeHp packet)
    {
        // TODO: HP bar UI
    }

    public void OnDead(S_Die packet)
    {
        //_animator.SetTrigger("Die");
    }

    public void OnRespawn(S_Respawn packet)
    {
        _agent.Warp(new Vector3(packet.PosInfo.PosX, packet.PosInfo.PosY, packet.PosInfo.PosZ));
        //_animator.SetTrigger("Respawn");
    }
}

