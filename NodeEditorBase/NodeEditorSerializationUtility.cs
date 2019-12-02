#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using KiwiKaleidoscope.Dialogue;

namespace NodeEditor
{

    [Serializable]
    public class WindowData
    {
        public string path;

        public float scaleFactor;

        public Vector2 gridOffset;

        public List<NodeData> nodes;

        public List<ConnectionData> connections;

        public WindowData(string path, float scaleFactor, Vector2 gridOffset, List<NodeData> nodes, List<ConnectionData> connections)
        {
            this.path = path;
            this.scaleFactor = scaleFactor;
            this.gridOffset = gridOffset;
            this.nodes = nodes;
            this.connections = connections;
        }
    }

    [Serializable]
    public class NodeData
    {
        public string id;
        public Vector2 pos;

        public NodeData(Node node)
        {
            id = node.GetNodeID();
            pos = node.rect.position;
        }
    }

    [Serializable]
    public class ConnectionData
    {
        public bool trueOut;
        public string startID;
        public string endID;

        public ConnectionData(Connection connection)
        {
            startID = connection.start.node.GetNodeID();
            endID = connection.end.node.GetNodeID();
        }
    }

    [Serializable]
    public class EditorHistory
    {
        public List<string> history = new List<string>();

        public EditorHistory()
        {
            history = new List<string>();
        }
    }

    
}
#endif