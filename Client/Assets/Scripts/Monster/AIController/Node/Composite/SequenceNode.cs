using Google.Protobuf.Protocol;
using UnityEngine;
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
}
//public class SequenceNode : CompositeNode
//{
//    private int _currentIdx = 0;

//    public override NodeStatus Execute(GameObject obj)
//    {
//        if (_currentIdx >= children.Count)
//            _currentIdx = 0;

//        for (int i = 0; i < children.Count; i++)
//        {
//            var status = children[i].Execute(obj);

//            if (status == NodeStatus.Failure)
//            {
//                if (i == 0 && _currentIdx > 0)
//                {
//                    ResetChildren();
//                }
//                _currentIdx = 0;
//                return NodeStatus.Failure;
//            }

//            if (status == NodeStatus.Running)
//            {
//                _currentIdx = i;
//                return NodeStatus.Running;
//            }
//        }

//        _currentIdx = 0;
//        ResetChildren();
//        return NodeStatus.Success;
//    }

//    private void ResetChildren()
//    {
//        foreach (var child in children)
//        {
//            if (child is CompositeNode composite)
//            {
//                ResetCompositeChildren(composite);
//            }
//        }
//    }

//    private void ResetCompositeChildren(CompositeNode composite)
//    {
//        foreach (var child in composite.children)
//        {
//            if (child is PlayAnimation playAnim)
//            {
//                Debug.Log($"Resetting PlayAnimation: {playAnim.name}");
//                playAnim.ClearAnimationRunState();
//            }
//            else if (child is CompositeNode nestedComposite)
//            {
//                ResetCompositeChildren(nestedComposite);
//            }
//        }
//    }
//}
