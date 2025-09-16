using System.Collections.Generic;
using UnityEngine;
using System;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

#if UNITY_EDITOR
using UnityEditor;
#endif

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
        Node node = CreateNodeInstance(data.Type); 
        if (!node) return null;

        node.name = data.Name;
        if (data.Properties != null)
        {
            using (var reader = data.Properties.CreateReader())
                JsonSerializer.CreateDefault().Populate(reader, node);
        }
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
        {
            Debug.Log($" BT_Builder 타입을 찾을 수 없음: {typeName}");
            return null;
        }

        Node node = ScriptableObject.CreateInstance(type) as Node;
        return node;
    }
}