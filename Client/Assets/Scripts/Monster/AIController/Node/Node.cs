using UnityEngine;
public enum NodeStatus
{
    Running,
    Success,
    Failure,
}

public abstract class Node : ScriptableObject
{
    protected NodeStatus _state; 
    public abstract void Enter(GameObject obj);
    public abstract NodeStatus Execute(GameObject obj);
    public abstract void Exit(GameObject obj, bool clear);

    public virtual Node Clone()
    {
        return Instantiate(this);
    }
}

public class BehaviorTree : ScriptableObject
{
    public Node rootNode;

    public Node Clone()
    {
        if (rootNode == null)
            return null;

        return rootNode.Clone();
    }
}
