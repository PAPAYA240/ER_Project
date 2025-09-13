using Data;
using Google.Protobuf.Protocol;
using System.Collections.Generic;
using UnityEngine;

public class PlayEffectNode : ActionNode, IStateChangeListener
{
    bool isEnd = false;
    public override NodeStatus Execute(GameObject owner)
    {
        MonsterController monster = owner.GetComponentInChildren<MonsterController>();
        if (monster == null)
            return NodeStatus.Failure;

        isEnd = true;
        if (monster.ObjInfo.MonsterType == MonsterType.Alpha)
        {
            monster = owner.GetComponentInChildren<MonsterController>();
            Transform handL = monster.FindInDescendants(monster.transform, "Fx_Hand_L");

            if (DataManager.MonsterSkillDict.TryGetValue(monster.Skill, out List<EffectData> data))
            {
                // TODO : Slash는 나중에 손 위치에 맞춰주기
                Managers.FX.PlayEffect(data, monster.transform, monster.TargetPosition, monster.transform.rotation);
                return NodeStatus.Success;
            }
        }
        else
        {
            if (DataManager.MonsterSkillDict.TryGetValue(monster.Skill, out List<EffectData> data))
            {
                Managers.FX.PlayEffect(data, monster.transform, monster.TargetPosition);
                return NodeStatus.Success;
            }
        }
        return NodeStatus.Running;
    }

    public void HandleStateChange(CreatureState newState)
    {
        isEnd = false;
    }
}
