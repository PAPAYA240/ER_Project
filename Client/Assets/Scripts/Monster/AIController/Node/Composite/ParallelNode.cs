using Google.Protobuf.Protocol;
using System.Collections.Generic;
using UnityEngine;


public class ParallelNode : CompositeNode, IStateChangeListener
{
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
    private void ResetChildren()
    {
        foreach (var child in children)
        {
            if (child is PlayAnimation playAnim)
                playAnim.Reset();
            // 다른 타입의 노드들도 필요하면 추가
        }
    }
    public void HandleStateChange(CreatureState newState, bool isClear = true)
    {
        if (isClear)
            _finished = true;
        else
            ResetChildren();
        ResetNode();
    }
}
