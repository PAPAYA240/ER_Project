using Data;
using Google.Protobuf.Protocol;
using System.Threading;
using UnityEngine;

public class SetMaterialNode : ActionNode
{
    public string strChangeMaterial;

    private Material changeMaterial = null;
    private Material originalMaterial = null;
    private Renderer monsterRenderer = null;

    public override void Enter(GameObject obj)
    {
        MonsterController monster = obj.GetComponentInChildren<MonsterController>();
        if (monster == null)
            return;

        monsterRenderer = monster.GetComponentInChildren<Renderer>();
        changeMaterial = Resources.Load<Material>(strChangeMaterial);
    }

    public override NodeStatus Execute(GameObject owner)
    {
        if (monsterRenderer == null || changeMaterial == null)
            return NodeStatus.Failure;

        if (originalMaterial == null)
            originalMaterial = monsterRenderer.material;

        if (originalMaterial == null)
            return NodeStatus.Failure;

        monsterRenderer.material = changeMaterial;
        return NodeStatus.Running;
    }

    public override void Exit(GameObject obj, bool clear)
    {
        if (monsterRenderer == null || originalMaterial == null)
            return;
        monsterRenderer.material = originalMaterial;
    }
}
