using UnityEngine;

public class IsSpawnedCheckNode : DecoratorNode
{
    public override NodeStatus Execute(GameObject agent)
    {
        MonsterController monsterState = agent.GetComponent<MonsterController>();
        if (monsterState != null && monsterState.isSpawned)
            return NodeStatus.Success;
     
        return NodeStatus.Failure;
    }
}
