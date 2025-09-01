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
}
