using System.Collections.Generic;
using UnityEngine;

// 여러 개의 자식을 가질 수 있게 하는 노드
// Sequence => 자식 노드를 순서대로 실행, 하나라도 실패하면 실패 반환, 모두 성공해야 하는 것들
// Selector => 자식 노드를 순서대로 실행, 하나라도 성공하면 성공 반환, 여러 개 중 하나만 성공하면 되는 것들
// 흐름 제어 노드

public abstract class CompositeNode : Node
{
    public List<Node> children = new List<Node>();
    protected Node _runningChild = null;
    public void AddChild(Node child)
    {
        children.Add(child);
    }
    public override Node Clone()
    {
        CompositeNode clone = base.Clone() as CompositeNode;
        clone.children = children.ConvertAll(c => c.Clone());
        return clone;
    }
}

