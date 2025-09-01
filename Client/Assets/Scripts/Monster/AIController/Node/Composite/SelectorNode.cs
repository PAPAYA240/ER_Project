using UnityEngine;


public class SelectorNode : CompositeNode
{
    private int _currentIdx = 0;

    public override NodeStatus Execute(GameObject obj)
    {
        if (_currentIdx >= children.Count)
            _currentIdx = 0;

        for (int i = _currentIdx; i < children.Count; i++)
        {
            _currentIdx = i;
            var status = children[i].Execute(obj);

            if(status == NodeStatus.Running)
                return NodeStatus.Running;
            if (status == NodeStatus.Success)
            {
                // 성공 시에 다시 시작 및 초기화
                _currentIdx = 0;
                return NodeStatus.Success;
            }
        }
        _currentIdx = 0;
        return NodeStatus.Failure;
    }
}
