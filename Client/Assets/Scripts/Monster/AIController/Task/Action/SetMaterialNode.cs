using Data;
using Google.Protobuf.Protocol;
using System.Threading;
using UnityEngine;

public class SetMaterialNode : ActionNode
{
    public string _strChangeMaterial;

    private Material _changeMaterial = null;
    private Material _originalMaterial = null;
    private Renderer _monsterRenderer = null;

    public override void Enter(GameObject owner)
    {
        MonsterController monster = owner.GetComponentInChildren<MonsterController>();
        if (monster == null)
        {
            return;
        }

        _monsterRenderer = monster.GetComponentInChildren<Renderer>();
        _changeMaterial = Resources.Load<Material>(_strChangeMaterial);
    }

    public override NodeStatus Execute(GameObject owner)
    {
        if (_monsterRenderer == null || _changeMaterial == null)
            return NodeStatus.Failure;

        if (_originalMaterial == null)
            _originalMaterial = _monsterRenderer.material;

        if (_originalMaterial == null)
            return NodeStatus.Failure;

        _monsterRenderer.material = _changeMaterial;

        return NodeStatus.Running;
    }

    public override void Exit(GameObject owner, bool clear)
    {
        if (_monsterRenderer == null || _originalMaterial == null)
            return;

        _monsterRenderer.material = _originalMaterial;
    }
}
