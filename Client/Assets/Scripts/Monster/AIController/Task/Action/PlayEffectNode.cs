using Data;
using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
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

        if (DataManager.MonsterEffectDict.TryGetValue(monster.Skill, out List<EffectData> data))
        {
            if (monster.State == CreatureState.Appear)
            {
                string targetName = $"{monster.Type}_{monster.State}";
                List<EffectData> targetEffects = data.Where(effect => effect.prefabName == targetName ).ToList();
                Managers.FX.PlayEffect(monster.ObjInfo.ObjectId, targetEffects, monster.transform, monster.TargetPosition, monster.TargetPosition);
            }
            else
            {
                List<EffectData> nonHitEffects = data.Where(effect =>
                string.IsNullOrEmpty(effect.prefabName) ||
                effect.prefabName.IndexOf("hit", StringComparison.OrdinalIgnoreCase) < 0 ).ToList();

                if (nonHitEffects.Count > 0)
                {
                    Managers.FX.PlayEffect(monster.ObjInfo.ObjectId, nonHitEffects, monster.transform, monster.TargetPosition, monster.TargetPosition);
                }
            }
        }

      

        return NodeStatus.Success;
    }

    public override void Exit(GameObject obj, bool clear)
    {
    }
}
