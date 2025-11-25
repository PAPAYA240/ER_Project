using Google.Protobuf.Protocol;
using UnityEngine;

public class DeadConditionNode : DecoratorNode
{
    private bool _isStop = false;
    public override NodeStatus Execute(GameObject agent)
    {
        MonsterController monster = agent.GetComponentInChildren<MonsterController>();

        if (monster?.State == CreatureState.Dead && !_isStop)
        {
            _isStop = true;
            return NodeStatus.Success;
        }
        else
            return NodeStatus.Failure;
    }
}