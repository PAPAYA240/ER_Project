using Google.Protobuf.Protocol;
using UnityEngine;

public class CheckSpawnCondition : DecoratorNode
{
    public override NodeStatus Execute(GameObject agent)
    {
        MonsterController monsterState = agent.GetComponent<MonsterController>();

        if (monsterState?.State == CreatureState.Appear)
            return NodeStatus.Success;
        else
            return NodeStatus.Failure;
            //return? NodeStatus.Success : NodeStatus.Failure;
    }
}
