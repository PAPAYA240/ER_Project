using Google.Protobuf.Protocol;
using UnityEngine;

public class SkillConditionNode : DecoratorNode
 {
    public MonsterSkill skillId;
    public override NodeStatus Execute(GameObject agent)
     {
        MonsterController controller = agent.GetComponentInChildren<MonsterController>();

        if (controller?.Skill != skillId || controller?.State != CreatureState.Skill)
            return NodeStatus.Failure;
        else
            return NodeStatus.Success;
    }
 }