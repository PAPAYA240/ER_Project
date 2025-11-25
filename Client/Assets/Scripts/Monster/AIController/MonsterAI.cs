using Google.Protobuf.Protocol;
using System.Collections.Generic;
using UnityEngine;

public class MonsterAI : MonoBehaviour
{
    private Node _rootNode; // List가 아닌 단일 루트
    private float _tickInterval = 0.2f;
    private float _timer = 0f;
    private MonsterController _controller;
    private List<IStateChangeListener> _stateListeners = new List<IStateChangeListener>();

    public float PrevHp = 0;

    void Start()
    {
        _controller = GetComponentInChildren<MonsterController>();
        CreateBehaviorTree();
        FindAllListeners(_rootNode);

        if (_controller != null)
        {
            foreach (var listener in _stateListeners)
                _controller.OnStateChanged += listener.HandleStateChange;
        }
        PrevHp = _controller.Hp;
    }

    void Update()
    {
         _timer += Time.deltaTime;
         if (_timer >= _tickInterval)
         {
             _timer = 0f;
             _rootNode?.Execute(gameObject); 
         }
    }


    private void CreateBehaviorTree()
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>("Data/MonsterData/MonsterBehaviorTrees");
        if (jsonAsset == null)
        {
            Debug.LogError("MonsterBehaviorTrees.json 파일을 찾을 수 없습니다!");
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
            Debug.Log($"[{_controller.Type}] Loaded {rootSelector.children.Count} behaviors");
        }
    }

    private void FindAllListeners(Node node)
    {
        if (node == null) return;

        if (node is IStateChangeListener listener)
            _stateListeners.Add(listener);

        if (node is CompositeNode composite)
        {
            foreach (var child in composite.children)
                FindAllListeners(child);
        }
    }
}