using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class PrioritySelectorNode : CompositeNode
{
    public override void Enter(GameObject owner)
    {
        foreach (var child in children)
        {
            child.Enter(owner);
        }
    }

    public override NodeStatus Execute(GameObject obj)
    {
        foreach (Node node in children)
        {
            switch (node.Execute(obj))
            {
                case NodeStatus.Success:
                    _state = NodeStatus.Success;
                    return _state;

                case NodeStatus.Running:
                    _state = NodeStatus.Running;
                    return _state;

                case NodeStatus.Failure:
                    continue; 
            }
        }

        _state = NodeStatus.Failure;
        return _state;
    }

    public override void Exit(GameObject owner, bool clear)
    {
        foreach (var child in children)
        {
            child.Exit(owner, clear);
        }
    }
}
