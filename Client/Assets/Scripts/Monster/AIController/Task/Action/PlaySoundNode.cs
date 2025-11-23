using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaySoundNode : ActionNode, IStateChangeListener
{
    public List<string> soundPaths;

    public override NodeStatus Execute(GameObject obj)
    {
        MonsterController monster = obj.GetComponentInChildren<MonsterController>();
        if (monster == null)
            return NodeStatus.Failure;

        monster.StartCoroutine(PlaySequentially(monster));

        return NodeStatus.Success;
    }

    private IEnumerator PlaySequentially(MonsterController monster)
    {
        foreach (string name in soundPaths)
        {
            string fullPath = $"Monster/{monster.Type}_{name}";
            Vector3 position = monster.transform.position;
            float duration = Managers.Sound.Play3D(fullPath, position);
            yield return new WaitForSeconds(duration);
        }
    }
    public void HandleStateChange(CreatureState newState, bool isClear = true)
    {
    }
}
