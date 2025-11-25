using Data;
using Google.Protobuf.Protocol;
using System.Collections.Generic;
using UnityEngine;

public class PlayEffectNode : ActionNode
{
    public override void Enter(GameObject obj)
    {
    }

    public override NodeStatus Execute(GameObject owner)
    {
        MonsterController monster = owner.GetComponentInChildren<MonsterController>();
         if (monster == null)
            return NodeStatus.Failure;

        if (DataManager.MonsterSkillDict.TryGetValue(monster.Skill, out List<EffectData> data))
        {
            Managers.FX.PlayEffect(monster.ObjInfo.ObjectId, data, monster.transform, monster.TargetPosition, monster.TargetPosition);
        }

        return NodeStatus.Success;
    }

    public override void Exit(GameObject obj, bool clear)
    {
    }
}
