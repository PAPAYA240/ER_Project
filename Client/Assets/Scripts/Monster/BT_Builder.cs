using System.Collections.Generic;
using UnityEngine;

public class BehaviorTreeBuilder
{
    private Node _root;
    private Stack<CompositeNode> _parentNodeStack = new Stack<CompositeNode>();

    /// <summary>
    /// Selector 노드를 현재 트리에 추가하고, 이 노드를 새로운 부모로 설정합니다.
     /// </summary>
     /// <param name="name">노드의 이름 (디버깅용)</param>
     /// <returns>메서드 체이닝을 위한 빌더 자신</returns>
     public BehaviorTreeBuilder Selector(string name = "Selector")
    {
        var selector =
ScriptableObject.CreateInstance<SelectorNode>();
        selector.name = name;

        AddNodeToParent(selector);
        _parentNodeStack.Push(selector);
        return this;
    }

    /// <summary>
    /// Sequence 노드를 현재 트리에 추가하고, 이 노드를 새로운 부모로 설정합니다.
     /// </summary>
     /// <param name="name">노드의 이름 (디버깅용)</param>
     /// <returns>메서드 체이닝을 위한 빌더 자신</returns>
     public BehaviorTreeBuilder Sequence(string name = "Sequence")
    {
        var sequence = ScriptableObject.CreateInstance<SequenceNode>();
        sequence.name = name;

        AddNodeToParent(sequence);
        _parentNodeStack.Push(sequence);
        return this;
    }

    /// <summary>
    /// Action 노드를 현재 부모 노드의 자식으로 추가합니다.
    /// </summary>
    /// <param name="action">추가할 Action 노드</param>
    /// <returns>메서드 체이닝을 위한 빌더 자신</returns>
    public BehaviorTreeBuilder Action(ActionNode action)
    {
        AddNodeToParent(action);
        return this;
    }

    /// <summary>
    /// Decorator 노드를 현재 부모 노드의 자식으로 추가합니다.
    /// </summary>
    /// <param name="decorator">추가할 Decorator 노드</param>
    /// <returns>메서드 체이닝을 위한 빌더 자신</returns>
    public BehaviorTreeBuilder Condition(DecoratorNode decorator)
    {
        AddNodeToParent(decorator);
        return this;
    }
    /// <summary>
    /// 현재 작업 중인 Composite(Selector/Sequence) 노드를 끝내고, 한 단계 위 부모로 돌아갑니다.
     /// </summary>
     /// <returns>메서드 체이닝을 위한 빌더 자신</returns>
    public BehaviorTreeBuilder End()
    {
        if (_parentNodeStack.Count > 0)
        {
            _parentNodeStack.Pop();
        }
        return this;
    }

    /// <summary>
    /// 최종적으로 완성된 행동 트리의 루트 노드를 반환합니다.
    /// </summary>
    /// <returns>완성된 트리의 루트 노드</returns>
    public Node Build()
    {
        // 스택에 남아있는 모든 부모 설정을 클리어합니다.
        while (_parentNodeStack.Count > 0)
        {
            _parentNodeStack.Pop();
        }
        return _root;
    }

    // 내부 헬퍼 메서드: 현재 부모 노드에 자식 노드를 추가합니다.
    private void AddNodeToParent(Node node)
    {
        if (_parentNodeStack.Count > 0)
        {
            // 스택에 부모가 있다면, 현재 최상위 부모의 자식으로
        
            _parentNodeStack.Peek().children.Add(node);
        }
        else
         {
              _root = node;
         }
     }
}