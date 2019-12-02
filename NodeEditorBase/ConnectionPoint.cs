#if UNITY_EDITOR
using UnityEngine;

namespace NodeEditor
{
    public enum ConnectionType { In, Out }

    public class ConnectionPoint
    {
        //General connection point variables.
        public Rect rect;
        public Node node;
        public ConnectionType connectionType;

        //Button logic and visual style.
        protected System.Action<ConnectionPoint> onClick;
        protected GUIStyle connectorStyle;

        public ConnectionPoint(Node node, ConnectionType type, System.Action<ConnectionPoint> onClick)
        {
            rect = new Rect(0, 0, 10, 20);
            //Set variables.
            this.node = node;
            this.onClick = onClick;
            connectionType = type;
            //Set the style.
            if(connectionType == ConnectionType.In)
            {
                connectorStyle = NodeEditorStyles.InPoint;
            }
            else
            {
                connectorStyle = NodeEditorStyles.OutPoint;
            }
        }

        public virtual void DrawConnector(float rectPosY, GUIStyle style = null)
        {

            //Connectors vertical position is fed into the function since it'll be called by the node.
            rect.y = rectPosY;

            //Pick the X position.
            if(connectionType == ConnectionType.In)
            {
                rect.x = node.rect.x - rect.width;
            }
            else
            {
                rect.x = node.rect.x + node.rect.width;
            }
            //If connector (using base style or passed in one) is pressed, invoke the OnClick
            if (GUI.Button(rect, "", (style == null)? connectorStyle : style))
            {
                onClick?.Invoke(this);
            }
        }
    }

    //Container to hold all connection points in case they are requested from a node.
    public class ConnectionPoints
    {
        public ConnectionPoint inPoint = null;
        public ConnectionPoint outPoint = null;
        public ConnectionPoint trueOutPoint = null;
        public ConnectionPoint falseOutPoint = null;

        public ConnectionPoints(ConnectionPoint inPoint, ConnectionPoint outPoint)
        {
            this.inPoint = inPoint;
            this.outPoint = outPoint;
        }

        public ConnectionPoints(ConnectionPoint inPoint, ConnectionPoint trueOutPoint, ConnectionPoint falseOutPoint)
        {
            this.inPoint = inPoint;
            this.trueOutPoint = trueOutPoint;
            this.falseOutPoint = falseOutPoint;
        }
    }
}
#endif