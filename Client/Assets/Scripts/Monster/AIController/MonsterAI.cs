using Google.Protobuf.Protocol;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public class MonsterAI : MonoBehaviour
{
    private Node _rootNode; // List가 아닌 단일 루트
    private float _tickInterval = 0.2f;
    private float _timer = 0f;
    private MonsterController _controller;

    public float PrevHp = 0;

    void Start()
    {
        _controller = GetComponentInChildren<MonsterController>();
        if (_controller == null)
        {
            Debug.LogWarning($"MonsterAI : {_controller.Type} Controller 찾기 실패");
            return;
        }

        CreateBehaviorTree();
        if (!FindAllListeners(_rootNode))
        {
            Debug.LogWarning($"MonsterAI : {_controller.Type} 행동 트리 노드 찾기 실패");
            return;
        }

        _controller.OnStateChanged += OnExecute;
        PrevHp = _controller.Hp;
        _rootNode?.Enter(gameObject);
    }

    void Update()
    {
         _timer += Time.deltaTime;
         if (_timer >= _tickInterval)
         {
             _timer = 0f;
             NodeStatus status = _rootNode.Execute(gameObject);

            if (status == NodeStatus.Success)
            {
                OnExecute( true);
            }
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
        }
    }

    private bool FindAllListeners(Node node)
    {
        if (node == null)
            return false ;

        if (node is CompositeNode composite)
        {
            foreach (var child in composite.children)
                FindAllListeners(child);
        }

        return (_rootNode != null) ? true : false;
    }
}