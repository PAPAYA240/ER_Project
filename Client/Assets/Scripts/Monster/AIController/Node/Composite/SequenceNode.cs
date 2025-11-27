using UnityEngine;

public class SequenceNode : CompositeNode
{
    private int _currentIdx = 0;

    public override void Enter(GameObject obj)
    {
        _currentIdx = 0;
        for (int i = _currentIdx; i < children.Count; i++)
        {
            children[i].Enter(obj);
        }
    }

    public override NodeStatus Execute(GameObject obj)
    {
        if (_currentIdx >= children.Count)
            _currentIdx = 0;
        
        for (int i = _currentIdx; i < children.Count; i++)
        {
            _currentIdx = i;
            var status = children[i].Execute(obj);

            if (status == NodeStatus.Running)
                return NodeStatus.Running;

            if (status == NodeStatus.Failure)
            {
                _currentIdx = 0;
                return NodeStatus.Failure;
            }
        }
        _currentIdx = 0;
        return NodeStatus.Success;
    }

    public override void Exit(GameObject obj, bool clear)
    {
        _currentIdx = 0;
        for (int i = _currentIdx; i < children.Count; i++)
        {
            children[i].Exit(obj, clear);
        }
    }
}

