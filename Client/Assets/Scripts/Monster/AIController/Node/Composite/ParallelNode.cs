using Google.Protobuf.Protocol;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

public class ParallelNode : CompositeNode
{
    private List<NodeStatus> _childStates;

    public override void Enter(GameObject obj)
    {
        if (_childStates == null || _childStates.Count != children.Count)
            _childStates = new List<NodeStatus>(new NodeStatus[children.Count]);

        for (int i = 0; i < _childStates.Count; i++)
            _childStates[i] = NodeStatus.Running;

        foreach (var child in children)
        {
            child.Enter(obj);
        }
    }

    public override NodeStatus Execute(GameObject obj)
    {
        bool allCompleted = true;
        bool anyFailed = false;
  
        for (int i = 0; i < children.Count; i++)
        {
            if (_childStates[i] == NodeStatus.Running)
            {
                _childStates[i] = children[i].Execute(obj);
            }

            if (_childStates[i] == NodeStatus.Running)
                allCompleted = false;

            else if (_childStates[i] == NodeStatus.Failure)
                anyFailed = true;
        }

        if (allCompleted)
            return anyFailed ? NodeStatus.Failure : NodeStatus.Success;

        return NodeStatus.Running;
    }

    public override void Exit(GameObject obj, bool clear)
    {
        foreach (var child in children)
            child.Exit(obj, clear);
    }
}
