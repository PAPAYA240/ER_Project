using Google.Protobuf.Protocol;
using System.Collections.Generic;
using UnityEngine;

public enum NodeStatus
{
    Running,
    Success,
    Failure,
}

public abstract class Node : ScriptableObject
{
    protected NodeStatus _state; 

    public NodeStatus NodeState => _state;
    public abstract void Enter(GameObject obj);
    public abstract NodeStatus Execute(GameObject obj);
    public abstract void Exit(GameObject obj, bool clear);

    public virtual Node Clone()
    {
        return Instantiate(this);
    }
}

public class BehaviorTree : ScriptableObject
{
    public Node rootNode;

    // 런타임용으로 트리 전체를 복제
    public Node Clone()
    {
        if (rootNode == null)
            return null;

        return rootNode.Clone();
    }
}
