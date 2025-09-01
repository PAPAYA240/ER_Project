using UnityEngine;

[CreateAssetMenu(fileName = "New Sequence", menuName = "BehaviorTree/Composites/Sequence")]
public class SequenceNode : CompositeNode
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

            if (status == NodeStatus.Failure)
            {
                // 실패 시 다시 시작
                _currentIdx = 0;
                return NodeStatus.Failure;
            }
        }
        _currentIdx = 0;
        return NodeStatus.Success;
    }
}
