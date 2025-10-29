using Google.Protobuf.Protocol;
using UnityEngine;

public class CheckSpawnCondition : DecoratorNode
{
    public override NodeStatus Execute(GameObject agent)
    {
        MonsterController monsterState = agent.GetComponent<MonsterController>();

        return (monsterState?.State == CreatureState.Appear) ? NodeStatus.Success : NodeStatus.Failure;
    }
}
