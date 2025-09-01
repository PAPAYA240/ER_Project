using UnityEngine;

public enum NodeStatus
{
    Running,
    Success,
    Failure,
}

public abstract class Node : ScriptableObject
{
    public abstract NodeStatus Execute(GameObject obj);

    public virtual Node Clone()
    {
        return Instantiate(this);
    }
}

public class BehaviorTree : ScriptableObject
{
    public Node rootNode;

    // 런타임용으로 트리 전체를 복제
    public Node Clone()
    {
        if (rootNode == null)
            return null;

        return rootNode.Clone();
    }
}
