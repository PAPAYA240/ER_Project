using Data;
using Google.Protobuf.Protocol;
using UnityEngine;

public class SetMaterialNode : ActionNode, IStateChangeListener
{
    public string strChangeMaterial;

    private Material changeMaterial = null;
    private Material originalMaterial = null;
    private Renderer monsterRenderer = null;
    public override NodeStatus Execute(GameObject owner)
    {
        MonsterController monster = owner.GetComponentInChildren<MonsterController>();
        if (monster == null)
            return NodeStatus.Failure;

        monsterRenderer = monster.GetComponentInChildren<Renderer>();
        changeMaterial = Resources.Load<Material>(strChangeMaterial);
        if (originalMaterial == null)
            originalMaterial = monsterRenderer.material;

        if (changeMaterial == null || monsterRenderer == null || originalMaterial == null)
            return NodeStatus.Failure;

        monsterRenderer.material = changeMaterial;
        return NodeStatus.Success;
    }

    public void HandleStateChange(CreatureState newState, bool isClear = true)
    {
        if (monsterRenderer == null || originalMaterial == null)
            return;

        monsterRenderer.material = originalMaterial;
    }
}
