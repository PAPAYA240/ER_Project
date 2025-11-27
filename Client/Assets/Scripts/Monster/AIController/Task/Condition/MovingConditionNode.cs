using Google.Protobuf.Protocol;
using UnityEngine;

public class MovingConditionNode : DecoratorNode
{
    public override void Enter(GameObject obj)
    {
    }

    public override NodeStatus Execute(GameObject agent)
    {
        MonsterController monster = agent.GetComponentInChildren<MonsterController>();

        return (monster?.State == CreatureState.Moving) ? NodeStatus.Success : NodeStatus.Failure;
    }

    public override void Exit(GameObject obj, bool clear)
    {
    }
}