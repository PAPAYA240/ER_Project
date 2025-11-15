using Data;
using Google.Protobuf.Protocol;
using System.Collections.Generic;
using UnityEngine;

public class PlayEffectNode : ActionNode, IStateChangeListener
{
    public override NodeStatus Execute(GameObject owner)
    {
         MonsterController monster = owner.GetComponentInChildren<MonsterController>();
         if (monster == null)
             return NodeStatus.Failure;

         if (monster.ObjInfo.Monster.MonsterType == MonsterType.Alpha)
         {
             monster = owner.GetComponentInChildren<MonsterController>();
             Transform handL = Util.FindChildByName(monster.transform, "Fx_Hand_L").transform;

             if (DataManager.MonsterSkillDict.TryGetValue(monster.Skill, out List<EffectData> data))
                 Managers.FX.PlayEffect(monster.ObjInfo.ObjectId, data, monster.transform, monster._targetPos, monster._targetPos, monster.transform.rotation);
         }
         else
         {
             if (DataManager.MonsterSkillDict.TryGetValue(monster.Skill, out List<EffectData> data))
             {
                 Managers.FX.PlayEffect(monster.ObjInfo.ObjectId, data, monster.transform, monster._targetPos, monster._targetPos);
             }
         }
         return NodeStatus.Success;
    }

    public void HandleStateChange(CreatureState newState, bool isClear = true)
    {
    }
}
