using System.Collections.Generic;
using UnityEngine;

public class MonsterAI : MonoBehaviour
{
    private Node _rootNode;
    private MonsterController _controller;

    private float _tickInterval = 0.2f;
    private float _timer = 0f;

    void Start()
    {
        _controller = GetComponentInChildren<MonsterController>();
        if (_controller == null)
            return;

        CreateBehaviorTree();

        if (!FindAllListeners(_rootNode))
            return;

        _controller.OnStateChanged += OnExecute;
        _rootNode?.Enter(gameObject);
    }

    void Update()
    {
         _timer += Time.deltaTime;
         if (_timer >= _tickInterval)
         {
             _timer = 0f;
             NodeStatus status = _rootNode.Execute(gameObject);
        }
    }

    private void OnExecute(bool clear)
    {
        _rootNode?.Exit(gameObject, clear);

        _rootNode?.Enter(gameObject);
    }

    private void Destroy()
    {
        _rootNode?.Exit(gameObject, true);
    }
    private void CreateBehaviorTree()
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>("Data/MonsterData/MonsterBehaviorTrees");
        if (jsonAsset == null)
        {
            return;
        }

        var builder = new BehaviorTreeBuilder();
        List<Node> trees = builder.BuildMultipleFromJson(jsonAsset.text, _controller.Type);

        if (trees.Count > 0)
        {
            PrioritySelectorNode rootSelector = ScriptableObject.CreateInstance<PrioritySelectorNode>();

            foreach (var tree in trees)
            {
                if (tree is PrioritySelectorNode selector)
                {
                    rootSelector.children.AddRange(selector.children);
                }
            }
            _rootNode = rootSelector;
        }
    }

    private bool FindAllListeners(Node node)
    {
        if (node == null)
            return false ;

        if (node is CompositeNode composite)
        {
            foreach (var child in composite.children)
            {
                FindAllListeners(child);
            }
        }
        return (_rootNode != null) ? true : false;
    }
}