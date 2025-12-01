using Google.Protobuf.Protocol;
using UnityEngine;

//public class HitConditionNode : DecoratorNode
//{
//    public override void Enter(GameObject obj)
//    {
//    }

//    public override NodeStatus Execute(GameObject agent)
//    {
//        MonsterController monster = agent.GetComponentInChildren<MonsterController>();
//        MonsterAI monsterAI = agent.GetComponentInChildren<MonsterAI>();

//        if (monsterAI?.PrevHp != monster.Hp)
//        {
//            if (monsterAI.PrevHp > monster.Hp)
//            {
//                monsterAI.PrevHp = monster.Hp;
//                return NodeStatus.Success;
//            }
//            monsterAI.PrevHp = monster.Hp;
//        }
//        return NodeStatus.Failure;
//    }

//    public override void Exit(GameObject obj, bool clear)
//    {
//    }
//}
