using Data;
using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayEffectNode : ActionNode
{
    MonsterController _monster;


    public override void Enter(GameObject owner)
    {
        MonsterController monster = owner.GetComponentInChildren<MonsterController>();
        if (monster == null)
            return;
    }

    public override NodeStatus Execute(GameObject owner)
    {
        if (DataManager.MonsterEffectDict.TryGetValue(_monster.Skill, out List<EffectData> data))
        {
            if (_monster.State == CreatureState.Appear)
            {
                string targetName = $"{_monster.Type}_{_monster.State}";
                List<EffectData> targetEffects =
                    data.Where(effect => effect.prefabName == targetName ).ToList();

                Managers.FX.PlayEffect(_monster.ObjInfo.ObjectId, targetEffects, _monster.transform, _monster.TargetPosition, _monster.TargetPosition);
            }
            else
            {
                List<EffectData> nonHitEffects = data.Where(effect =>
                string.IsNullOrEmpty(effect.prefabName) ||
                effect.prefabName.IndexOf("hit", StringComparison.OrdinalIgnoreCase) < 0 ).ToList();

                if (nonHitEffects.Count > 0)
                {
                    Managers.FX.PlayEffect(_monster.ObjInfo.ObjectId, nonHitEffects, _monster.transform, _monster.TargetPosition, _monster.TargetPosition);
                }
            }
        }

        return NodeStatus.Success;
    }

    public override void Exit(GameObject owner, bool clear) { }
}
