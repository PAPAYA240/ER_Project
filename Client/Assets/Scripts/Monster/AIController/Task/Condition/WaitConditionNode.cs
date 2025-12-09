using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;
using UnityEngine.AI;

public class WaitConditionNode : DecoratorNode
{
    private const string APPEAR_ANIM = "APPEAR";
    protected Animator _animator;
    public override void Enter(GameObject obj)
    {
        if (_animator == null)
            _animator = obj.GetComponentInChildren<Animator>();
    }

    public override NodeStatus Execute(GameObject agent)
    {
        MonsterController monster = agent.GetComponentInChildren<MonsterController>();

        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        int appearAnimHash = Animator.StringToHash(APPEAR_ANIM);
        bool isAppear = stateInfo.IsName(APPEAR_ANIM);

        return (monster?.State == CreatureState.Idle || isAppear) ? NodeStatus.Success : NodeStatus.Failure;
    }

    public override void Exit(GameObject obj, bool clear)
    {
    }
}