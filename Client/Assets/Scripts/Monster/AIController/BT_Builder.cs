using System.Collections.Generic;
using UnityEngine;
using System;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Google.Protobuf.Protocol;

[System.Serializable]
public class NodeData
{
    public string Type;
    public string TargetObject;
    public string Name;

    public JObject Properties;
    public List<NodeData> Children = new List<NodeData>();
}

[System.Serializable]
public class BehaviorTreeCollection
{
    public List<NodeData> behaviorTrees = new List<NodeData>();
}

public class BehaviorTreeBuilder
{
    private Node _root;
    private Stack<CompositeNode> _parentNodeStack = new Stack<CompositeNode>();


    public List<Node> BuildMultipleFromJson(string json, MonsterType type)
    {
        List<Node> nodes = new List<Node>();
        BehaviorTreeCollection collection = JsonConvert.DeserializeObject<BehaviorTreeCollection>(json);

        if (collection == null || collection.behaviorTrees == null)
        {
            //Debug.LogError("BehaviorTreeCollection 파싱 실패!");
            return nodes;
        }


        foreach (NodeData data in collection.behaviorTrees)
        {
            if (data.TargetObject == "Common")
            {
                Node node = CreateNodeRecursive(data, type);
                if (node != null)
                    nodes.Add(node);
                continue;
            }

            if (Enum.TryParse(data.TargetObject, true, out MonsterType targetType) && type == targetType)
            {
                Node node = CreateNodeRecursive(data, type);
                if (node != null)
                    nodes.Add(node);
                continue;
            }
        }
        return nodes;
    }

    private Node CreateNodeRecursive(NodeData data, MonsterType type)
    {
        if (!string.IsNullOrEmpty(data.TargetObject))
        {
            if (data.TargetObject != "Common")
            {
                if (!Enum.TryParse(data.TargetObject, true, out MonsterType targetType) || type != targetType)
                {
                    return null;
                }
            }
        }

        Node node = CreateNodeInstance(data.Type);
        if (node == null)
            return null;

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
                    Node childNode = CreateNodeRecursive(childData, type);
                    if (childNode != null)
                        compositeNode.AddChild(childNode);
                }
            }
        }

        return node;
    }

    private Node CreateNodeInstance(string typeName)
    {
        if (typeName == null)
            return null;

        Type type = Type.GetType(typeName);
        if (null == type)
        {
            //Debug.LogError($"BT_Builder 타입을 찾을 수 없음: {typeName}");
            return null;
        }

        Node node = ScriptableObject.CreateInstance(type) as Node;
        return node;
    }
}