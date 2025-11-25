using Google.Protobuf.Protocol;
using UnityEngine;

public class DeadConditionNode : DecoratorNode
{
    public override NodeStatus Execute(GameObject agent)
    {
        MonsterController monster = agent.GetComponentInChildren<MonsterController>();

        return (monster?.State == CreatureState.Dead) ? NodeStatus.Success : NodeStatus.Failure;
    }
}