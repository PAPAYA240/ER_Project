using Google.Protobuf.Protocol;
using System.Collections.Generic;
using UnityEngine;

public class ParallelNode : CompositeNode, IStateChangeListener
{
    private List<NodeStatus> _childStates;

    //bool _finished = false;
    public override NodeStatus Execute(GameObject obj)
    {
        if (_childStates == null)
            ResetNode();

        bool allCompleted = true;
        bool anyFailed = false;

        //if (_finished)
        //{
        //    _finished = false;
        //    return NodeStatus.Success;
        //}

        for (int i = 0; i < children.Count; i++)
        {
            if (_childStates[i] == NodeStatus.Running)
            {
                _childStates[i] = children[i].Execute(obj);
            }

            // 상태 체크
            if (_childStates[i] == NodeStatus.Running)
                allCompleted = false;
            else if (_childStates[i] == NodeStatus.Failure)
                anyFailed = true;
        }

        if (allCompleted)
        {
            ResetNode();
            return anyFailed ? NodeStatus.Failure : NodeStatus.Success;
        }

        return NodeStatus.Running;
    }

    private void ResetNode()
    {
        if (_childStates == null)
             _childStates = new List<NodeStatus>(new NodeStatus[children.Count]);

        for (int i = 0; i < children.Count; i++)
            _childStates[i] = NodeStatus.Running;
    }
    private void ResetChildren()
    {
        foreach (var child in children)
        {
            if (child is PlayAnimation playAnim)
                playAnim.ClearAnimationRunState();

        }
    }
    public void HandleStateChange(CreatureState newState, bool isClear = true)
    {
        if (isClear)
            ;// _finished = true;
        else
            ResetChildren();

        ResetNode();
    }
}
