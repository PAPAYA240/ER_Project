using Google.Protobuf.Protocol;
using UnityEngine;

public class IsKeyPressedNode : DecoratorNode
 {
    public MonsterSkill skillId;
    public override NodeStatus Execute(GameObject agent)
     {
        MonsterController monsterController = agent.GetComponentInChildren<MonsterController>();
        if(!monsterController || monsterController.Skill != skillId || monsterController.State != CreatureState.Skill)
       { 
            return NodeStatus.Failure;
        }
         Debug.Log($"{skillId}");
        return NodeStatus.Success;
    }
 }

//public class CheckConditionNode : DecoratorNode
//{
//    public string key;
//    public string op;
//    public string value;

//    public override NodeStatus Execute(GameObject owner)
//    {
//        if (op == "equal")
//        {
//             if (isSpawned.ToString().ToLower() == value.ToLower())
//                 return NodeStatus.Success;
//        }
//         return NodeStatus.Failure;
//    }
//}

