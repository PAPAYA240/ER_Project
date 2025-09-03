using System.Collections.Generic;
using UnityEngine;
using System;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

[System.Serializable]
public class NodeData
{
    public string Type;
    public string Name;
    public JObject Properties;
    public List<NodeData> Children = new List<NodeData>();
}

public class BehaviorTreeBuilder
{
    private Node _root;
    private Stack<CompositeNode> _parentNodeStack = new Stack<CompositeNode>();

    public Node BuildFromJson(string json)
    {
        NodeData rootData = JsonConvert.DeserializeObject<NodeData>(json);
        return CreateNodeRecursive(rootData);
    }

    private Node CreateNodeRecursive(NodeData data)
    {
        Node node = CreateNodeInstance(data.Type); // 이 번째 줄
        if (!node) return null;

        node.name = data.Name;
        if (data.Properties != null)
            JsonConvert.PopulateObject(data.Properties.ToString(), node);

        if (data.Children != null && data.Children.Count > 0)
        {
            if (node is CompositeNode compositeNode)
            {
                foreach (NodeData childData in data.Children)
                {
                    Node childNode = CreateNodeRecursive(childData);
                    if(childNode != null)
                        compositeNode.AddChild(childNode);
                }
            }
        }
        return node;
    }

    private Node CreateNodeInstance(string typeName)
    {
        Type type = Type.GetType(typeName);
        if (null == type)
            return null;

        Node node = ScriptableObject.CreateInstance(type) as Node;
        return node;
    }

#if UNITY_EDITOR
    public BehaviorTreeBuilder Selector(string name = "Selector")
    {
        var selector = ScriptableObject.CreateInstance<SelectorNode>();
        selector.name = name;

        AddNodeToParent(selector);
        _parentNodeStack.Push(selector);
        return this;
    }

     public BehaviorTreeBuilder Sequence(string name = "Sequence")
    {
        var sequence = ScriptableObject.CreateInstance<SequenceNode>();
        sequence.name = name;

        AddNodeToParent(sequence);
        _parentNodeStack.Push(sequence);
        return this;
    }
    public BehaviorTreeBuilder Action(ActionNode action)
    {
        AddNodeToParent(action);
        return this;
    }
    public BehaviorTreeBuilder Condition(DecoratorNode decorator)
    {
        AddNodeToParent(decorator);
        return this;
    }
    public BehaviorTreeBuilder End()
    {
        if (_parentNodeStack.Count > 0)
        {
            _parentNodeStack.Pop();
        }
        return this;
    }
    public Node Build()
    {
        while (_parentNodeStack.Count > 0)
        {
            _parentNodeStack.Pop();
        }
        return _root;
    }
    private void AddNodeToParent(Node node)
    {
        if (_parentNodeStack.Count > 0)
        {
            _parentNodeStack.Peek().children.Add(node);
        }
        else
         {
              _root = node;
         }
     }
#endif
}