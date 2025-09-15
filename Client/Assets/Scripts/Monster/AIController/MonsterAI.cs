using Google.Protobuf.Protocol;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MonsterController))]
public class MonsterAI : MonoBehaviour
{
    private List<Node> _rootNodes = new List<Node>();

    private float _tickInterval = 0.2f;
    private float _timer = 0f;

    private MonsterController _monsterController;
    private List<IStateChangeListener> _stateListeners = new List<IStateChangeListener>();

    void Start()
    {
        CreateBehaviorTree();

        _monsterController = GetComponentInChildren<MonsterController>();
        
        FindAllListeners(_rootNodes);

        if (_monsterController != null)
        {
            foreach (var listener in _stateListeners)
                _monsterController.OnStateChanged += listener.HandleStateChange;
        }
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer < _tickInterval)
            return;

        _timer = 0f;
        foreach (var rootNode in _rootNodes)
            rootNode.Execute(this.gameObject);
    }

    private List<Node> CreateBehaviorTree()
    {
        TextAsset[] jsonAssets = Resources.LoadAll<TextAsset>("Data/MonsterData/MonsterDataSkillList");

        var builder = new BehaviorTreeBuilder();
        foreach (var jsonAsset in jsonAssets)
        {
            Node rootNode = builder.BuildFromJson(jsonAsset.text);
            _rootNodes.Add(rootNode);
        }
        return _rootNodes;
    }
    void OnDisable()
    {
        if (_monsterController != null)
        {
            foreach (var listener in _stateListeners)
                _monsterController.OnStateChanged -= listener.HandleStateChange;
        }
    }

    private void FindAllListeners(List<Node> nodes)
    {
        foreach (var node in nodes)
        {
            if (node is IStateChangeListener listener)
                _stateListeners.Add(listener);

            if (node is CompositeNode composite)
                FindAllListeners(composite.children);
        }
    }
}