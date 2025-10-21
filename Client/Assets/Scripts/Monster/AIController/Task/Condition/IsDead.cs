using Google.Protobuf.Protocol;
using UnityEngine;

public class IsDead : DecoratorNode
{
    public override NodeStatus Execute(GameObject agent)
    {
        MonsterController monster = agent.GetComponentInChildren<MonsterController>();
        if (monster == null)
            return NodeStatus.Failure;

        if (monster.State != CreatureState.Dead)
            return NodeStatus.Failure;
        return (monster.State == CreatureState.Dead) ? NodeStatus.Success : NodeStatus.Failure;
    }
}