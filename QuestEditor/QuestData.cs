using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using NodeEditor;

public class QuestData : ScriptableObject
{
    public string path;

    public float scaleFactor;

    public Vector2 gridOffset;

    public List<NodeData> nodes;

    public List<ConnectionData> connections;

    public void SetData(string path, float scaleFactor, Vector2 gridOffset, List<NodeData> nodes, List<ConnectionData> connections)
    {
        this.path = path;
        this.scaleFactor = scaleFactor;
        this.gridOffset = gridOffset;
        this.nodes = nodes;
        this.connections = connections;
    }
}
