using Google.Protobuf.Protocol;
using UnityEngine;

public class IsKeyPressedNode : DecoratorNode
 {
    public MonsterSkill skillId;
    public override NodeStatus Execute(GameObject agent)
     {
        MonsterController monsterController = agent.GetComponentInChildren<MonsterController>();

        if (!monsterController || monsterController.Skill != skillId || monsterController.State != CreatureState.Skill)
        {
            return NodeStatus.Failure;
        }
         return NodeStatus.Success;
    }
 }



