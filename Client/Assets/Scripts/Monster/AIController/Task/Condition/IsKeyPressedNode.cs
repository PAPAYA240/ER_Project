using Google.Protobuf.Protocol;
using UnityEngine;
using static MonsterController;


public class IsKeyPressedNode : DecoratorNode
 {
     public MonsterSkill skillId = MonsterSkill.Attack1;

    // TODO : _child => 제어할 애니메이션을 집어넣을 예정
    public override NodeStatus Execute(GameObject agent)
     {
        MonsterController monsterController = agent.GetComponentInChildren<MonsterController>();
        if(!monsterController || monsterController.Skill != skillId || monsterController.State != CreatureState.Skill)
            return NodeStatus.Failure;
        return NodeStatus.Success;
    }
 }

