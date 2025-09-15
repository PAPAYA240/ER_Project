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

         if (monster.ObjInfo.MonsterType == MonsterType.Alpha)
         {
             monster = owner.GetComponentInChildren<MonsterController>();
             Transform handL = monster.FindInDescendants(monster.transform, "Fx_Hand_L");

             if (DataManager.MonsterSkillDict.TryGetValue(monster.Skill, out List<EffectData> data))
             {
                // TODO : Slash는 나중에 손 위치에 맞춰주기
                 Managers.FX.PlayEffect(data, monster.transform, monster._targetPos, monster.transform.rotation);
             }
         }
         else
         {
             if (DataManager.MonsterSkillDict.TryGetValue(monster.Skill, out List<EffectData> data))
             {
                 Managers.FX.PlayEffect(data, monster.transform, monster._targetPos);
             }
         }
         return NodeStatus.Success;
    }

    public void HandleStateChange(CreatureState newState)
    {
    }
}
