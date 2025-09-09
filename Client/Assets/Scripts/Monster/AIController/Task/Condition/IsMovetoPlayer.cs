using Google.Protobuf.Protocol;
using UnityEngine;

public class IsMovetoPlayer : DecoratorNode
{
    public override NodeStatus Execute(GameObject agent)
    {
        MonsterController monster = agent.GetComponentInChildren<MonsterController>();
        if (monster == null)
        {    return NodeStatus.Failure;}
        return (monster.State == CreatureState.Moving) ? NodeStatus.Success : NodeStatus.Failure;
    }
}