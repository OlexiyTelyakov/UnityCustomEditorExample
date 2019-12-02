#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NodeEditor
{
    /// <summary>
    /// Base class from which all other nodes will inherit.
    /// </summary>
    public abstract class Node
    {
        //Parent node.
        public List<Node> parentNodes;
        public List<Node> childNodes;
        //All nodes will have a rect so it is declared here.
        public Rect rect;
        //All nodes will be subject to scale, so width and height are declared here.
        protected float nodeWidth;
        protected float nodeHeight;
        //All nodes will have title and type, so its is declared here.
        protected float titleBarHeight = 20f;
        protected float typeBarHeight = 15f;

        public string nodeTitle;
        protected string nodeType;

        protected bool isDragged = false;

        /// <summary>
        /// Externally called method that draws GUI of the node.
        /// </summary>
        public abstract void DrawNode();

        /// <summary>
        /// Externally called method that checks if node has used any events.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public abstract bool ProcessEvents(Event e);

        protected virtual void RemoveNode(object obj)
        {
            Node node = obj as Node;
            //DialogueEditor.RemoveNode(node);
        }

        /// <summary>
        /// Checks if attempted connection is possible.
        /// Updates lists of parent and child nodes based on the passed in connection after its created.
        /// Can be intructed to update the baseline SO (default true).
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="updateBase"></param>
        /// <returns></returns>
        public abstract bool ConnectionAdded(Connection connection, bool updateBase = true);

        /// <summary>
        /// Updates lists of parent and child nodes based on the passed in connection after its removed.
        /// Can be intructed to update the baseline SO (default true).
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="updateBase"></param>
        public abstract void ConnectionRemoved(Connection connection, bool updateBase = true);

        protected abstract void CalculateScaling();

        //Drag is the same across all nodes, so its implemented at base.
        public void Drag(Vector2 delta)
        {
            rect.position += delta;
        }

        //Will return the various scriptable objects that nodes lower in the inheritance use for data.
        public abstract ScriptableObject GetValue();

        public abstract string GetNodeID();

        public abstract ConnectionPoints GetConnectionPoints();
    }
}
#endif