using System.Collections.Generic;
using UnityEngine;

public class MonsterAI : MonoBehaviour
{
    private Node _rootNode; // List가 아닌 단일 루트
    private float _tickInterval = 0.2f;
    private float _timer = 0f;
    private MonsterController _monsterController;
    private List<IStateChangeListener> _stateListeners = new List<IStateChangeListener>();

    public float PrevHp = 0;
    void Start()
    {
        _monsterController = GetComponentInChildren<MonsterController>();
        CreateBehaviorTree();
        FindAllListeners(_rootNode);

        if (_monsterController != null)
        {
            foreach (var listener in _stateListeners)
                _monsterController.OnStateChanged += listener.HandleStateChange;
        }
        PrevHp = _monsterController.Hp;
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= _tickInterval)
        {
            _timer = 0f;
            _rootNode?.Execute(gameObject); // 단일 루트만 실행
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
        List<Node> trees = builder.BuildMultipleFromJson(jsonAsset.text, _monsterController.Type);

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
            Debug.Log($"[{_monsterController.Type}] Loaded {rootSelector.children.Count} behaviors");
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