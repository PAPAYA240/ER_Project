using Google.Protobuf.Protocol;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

public class ParallelNode : CompositeNode, IStateChangeListener
{
    private int _successIdx = 0;
    private int _failureIdx = 0;

    private List<NodeStatus> _childStates;

    bool _finished = false;
    public override NodeStatus Execute(GameObject obj)
    {
        if (_childStates == null)
            ResetNode();

        if (_finished)
        {
            _finished = false;
            return NodeStatus.Success;
        }

        for (int i = 0; i < children.Count; i++)
        {
            // 한 번 성공한 건 재실행하지 않고 대기
            if (_childStates[i] == NodeStatus.Running)
                _childStates[i] = children[i].Execute(obj);
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

    public void HandleStateChange(CreatureState newState)
    {
        _finished = true;
        ResetNode();
    }
}
