#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//UNITY WILL NOT BUILD WITH UNITY EDITOR NAMESPACE IN USE.
using UnityEditor;

namespace NodeEditor
{
    public class Connection
    {
        public ConnectionPoint start;
        public ConnectionPoint end;

        private System.Action<Connection> OnClickRemoveConnection;

        public Connection(ConnectionPoint start, ConnectionPoint end, System.Action<Connection> OnClickRemoveConnection)
        {
            this.start = start;
            this.end = end;
            this.OnClickRemoveConnection = OnClickRemoveConnection;
        }

        public void DrawConnection()
        {
            //If connection is incomplete, return.
            if (start == null || end == null) return;
            //Otherwise draw a bezier curve and a button to delete the connection.
            Handles.DrawBezier(start.rect.center, end.rect.center,
                start.rect.center - Vector2.left * 50f, end.rect.center + Vector2.left * 50f,
                Color.black, null, 3f);

            if(Handles.Button((start.rect.center + end.rect.center)/2f, Quaternion.identity, 4f, 8f, Handles.RectangleHandleCap))
            {
                OnClickRemoveConnection?.Invoke(this);
            }
        }
    }
}
#endif