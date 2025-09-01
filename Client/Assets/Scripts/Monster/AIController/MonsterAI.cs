using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MonsterController))]
public class MonsterAI : MonoBehaviour
{
    // 루트 노드
    private List<Node> _rootNodes = new List<Node>();

    private float _tickInterval = 0.2f;
    private float _timer = 0f;

    void Start()
    {
        CreateBehaviorTree();
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
        TextAsset[] jsonAssets = Resources.LoadAll<TextAsset>("Data/MonsterData");

        // 2. 불러온 모든 JSON 파일을 순회
        var builder = new BehaviorTreeBuilder();
        foreach (var jsonAsset in jsonAssets)
        {
            // 3. 각 JSON 파일로부터 행동 트리를 생성하고 리스트에 추가
            Node rootNode = builder.BuildFromJson(jsonAsset.text);
            _rootNodes.Add(rootNode);
        }
        return _rootNodes;
    }
}