using Google.Protobuf.Protocol;
using UnityEngine;

public class SpawnConditionNode : DecoratorNode
{
    public override void Enter(GameObject obj)
    {
    }

    public override NodeStatus Execute(GameObject agent)
    {
        MonsterController monsterState = agent.GetComponent<MonsterController>();

        if (monsterState?.State == CreatureState.Appear)
            return NodeStatus.Success;
        else
            return NodeStatus.Failure;
    }

    public override void Exit(GameObject obj, bool clear)
    {
    }
}
