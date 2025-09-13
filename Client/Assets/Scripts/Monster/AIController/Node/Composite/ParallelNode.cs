using Google.Protobuf.Protocol;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

public class ParallelNode : CompositeNode, IStateChangeListener
{
    private int _successIdx = 0;
    private int _failureIdx = 0;

    private List<NodeStatus> _childStates;

    public override NodeStatus Execute(GameObject obj)
    {
        if (_childStates == null)
            ResetNode();

        // Effect Success , Animation Runing, ResetNode()를 잘못해주나?
        for (int i = 0; i < children.Count; i++)
        {
            // 한 번 성공한 건 재실행하지 않고 대기
            if (_childStates[i] == NodeStatus.Running)
            {
                _childStates[i] = children[i].Execute(obj);
                switch (_childStates[i])
                {
                    case NodeStatus.Success:
                        _successIdx++;
                        break;
                    case NodeStatus.Failure:
                        _failureIdx++;
                        break;
                    case NodeStatus.Running:
                        break;
                }
            }
        }

           
        if (isResult[0] == true)
            return NodeStatus.Failure;
        if(isResult[1] == true)
            return NodeStatus.Success;
    
        return NodeStatus.Running;
    }

    private void ResetNode()
    {
        if (_childStates == null)
             _childStates = new List<NodeStatus>(new NodeStatus[children.Count]);
        for (int i = 0; i < children.Count; i++)
            _childStates[i] = NodeStatus.Running;
    }
    bool[] isResult = { false, false };
    public void HandleStateChange(CreatureState newState)
    {
        if (_failureIdx > 0)
        {
            _failureIdx = 0;
            ResetNode();
            isResult[0] = true;
        }

        if (_successIdx >= children.Count)
        {
            _successIdx = 0;
            ResetNode();
            isResult[1] = true;
        }

    }
}
