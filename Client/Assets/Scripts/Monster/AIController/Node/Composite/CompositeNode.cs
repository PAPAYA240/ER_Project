using System.Collections.Generic;


public abstract class CompositeNode : Node
{
    public List<Node> children = new List<Node>();
    protected Node _runningChild = null;

    public void AddChild(Node child)
    {
        children.Add(child);
    }
    public override Node Clone()
    {
        CompositeNode clone = base.Clone() as CompositeNode;
        clone.children = children.ConvertAll(c => c.Clone());

        return clone;
    }
}

