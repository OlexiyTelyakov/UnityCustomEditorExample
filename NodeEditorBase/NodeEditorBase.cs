#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace NodeEditor
{
    public class NodeEditorBase : EditorWindow
    {
        //All handled items.
        protected List<Node> nodes;
        protected List<Connection> connections;

        protected Rect toolbarRect;
        protected Vector2 gridOffset;
        protected Vector2 drag;

        protected float toolbarHeight = 20f;

        protected ConnectionPoint selectedInPoint;
        protected ConnectionPoint selectedOutPoint;

        #region ConnectionUtility

        protected void OnClickInPoint(ConnectionPoint inPoint)
        {
            selectedInPoint = inPoint;

            if (selectedOutPoint != null)
            {
                if (selectedOutPoint.node != selectedInPoint.node)
                {
                    CreateConnection();
                    ClearConnectionSelection();
                }
                else
                {
                    ClearConnectionSelection();
                }
            }
        }

        protected void OnClickOutPoint(ConnectionPoint outPoint)
        {
            selectedOutPoint = outPoint;

            if (selectedInPoint != null)
            {
                if (selectedOutPoint.node != selectedInPoint.node)
                {
                    CreateConnection();
                    ClearConnectionSelection();
                }
                else
                {
                    ClearConnectionSelection();
                }
            }
        }

        protected void CreateConnection()
        {
            //Since duplicates are allowed, make sure you can't create an infinite chain
            if (selectedInPoint.node.GetNodeID() == selectedOutPoint.node.GetNodeID())
            {
                //Issue warning
                Debug.LogWarning("You cannot chain same objects together. It will case a loop. That will be bad");
                return;
            }
            //Initialize the list if necessary.
            if (connections == null) connections = new List<Connection>();
            //Create the new connection
            Connection newConnection = new Connection(selectedOutPoint, selectedInPoint, RemoveConnection);
            //If creation connection is allowed (simultaneuously updating the nodes):
            if (newConnection.start.node.ConnectionAdded(newConnection) && newConnection.end.node.ConnectionAdded(newConnection))
            {
                //Add it to the connections list.
                connections.Add(newConnection);
            }
        }

        protected void ClearConnectionSelection()
        {
            selectedInPoint = null;
            selectedOutPoint = null;
        }

        protected void RemoveConnection(Connection connection)
        {
            //If collections list is initialized and contains the connection:
            if (connections != null && connections.Contains(connection))
            {
                //Update the nodes
                connection.start.node.ConnectionRemoved(connection);
                connection.end.node.ConnectionRemoved(connection);
                //Remove the connection.
                connections.Remove(connection); //The assumption here is that the garbage collection will get rid of it?
            }
        }

        //Draws a bezier to the mouse position if a connector has been selected.
        protected void DrawConnectionLine(Event e)
        {
            if (selectedInPoint != null && selectedOutPoint == null)
            {
                Handles.DrawBezier(selectedInPoint.rect.center, e.mousePosition,
                    selectedInPoint.rect.center + Vector2.left * 50f, e.mousePosition - Vector2.left * 50f,
                    Color.black, null, 3f);
            }
            if (selectedInPoint == null && selectedOutPoint != null)
            {
                Handles.DrawBezier(selectedOutPoint.rect.center, e.mousePosition,
                    selectedOutPoint.rect.center - Vector2.left * 50f, e.mousePosition + Vector2.left * 50f,
                    Color.black, null, 3f);
            }
            GUI.changed = true;
        }
        #endregion

        protected void ProcessNodeEvents(Event e)
        {
            //Send the event along for each node and check if the process it.
            if (nodes != null)
            {
                for (int i = nodes.Count - 1; i >= 0; i--)
                {
                    bool guiChanged = nodes[i].ProcessEvents(e);
                    if (guiChanged) GUI.changed = true;
                }
            }
        }

        //Called when there is drag on the grid space
        protected void OnDrag(Vector2 delta)
        {
            //Set drag as delta.
            drag = delta;
            //Drag all the nodes.
            if (nodes != null)
            {
                foreach (Node node in nodes)
                {
                    node.Drag(delta);
                }
            }
            //Force a GUI repaint.
            GUI.changed = true;
        }

        //Callback for the node focus menu.
        protected void FocusNode(object clb)
        {
            int index = (int)clb;

            Vector2 difference = new Vector2(position.width / 2, (position.height + toolbarHeight) / 2) - nodes[index].rect.center;
            foreach (Node n in nodes)
            {
                n.rect.position += difference;
            }
            gridOffset = Vector2.zero;
        }

        protected void DrawGrid(float gridInterval, float gridOpacity)
        {
            //gridInterval *= scaleFactor;
            int widthInterval = Mathf.CeilToInt(position.width / gridInterval);
            int heightInterval = Mathf.CeilToInt(position.height / gridInterval);

            Color gridColor = Color.gray;

            //Start handles - in this case lines for the grid BG.
            Handles.BeginGUI();
            GUI.depth = 1;
            //Add diminished drag to the offset to smooth it out.
            gridOffset += drag * 0.5f;
            //Contain the offset within the grid.
            Vector3 newOffset = new Vector3(gridOffset.x % gridInterval, gridOffset.y % gridInterval, 0);
            //Set the color to correct opacity.
            Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);

            //Draw vertical lines(across width of the window).
            for (int i = 0; i < widthInterval; i++)
            {
                //0,0 coordinate of the window is its top left corner, increasing downwards.
                //Hense we start at whatever grid interwal on the X, but slightly above it on the Y, adding the offset (similar to shifting a sine curve).
                //Line is then drawn to the edge of the window (position.height).

                Handles.DrawLine(new Vector3(gridInterval * i, -gridInterval, 0) + newOffset, new Vector3(gridInterval * i, position.height + gridInterval, 0f) + newOffset);
            }
            //Draw horizontal lines(across height of the window).
            for (int i = 0; i < heightInterval; i++)
            {
                Handles.DrawLine(new Vector3(-gridInterval, gridInterval * i, 0) + newOffset, new Vector3(position.width + gridInterval, gridInterval * i, 0f) + newOffset);
            }
            //Reset color
            Handles.color = Color.white;
            //Stop drawing handles.
            Handles.EndGUI();
        }

        /// <summary>
        /// Allows for objects to be dragged and dropped into the editor window.
        /// Found on Unity Forums: https://forum.unity.com/threads/working-with-draganddrop-for-a-custom-editor-window.94192/
        /// </summary>
        /// <param name="title"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        public object[] DropZone(string title, int w, int h)
        {
            //Not entirely sure why it needs to create the box
            GUILayout.Box(title, GUILayout.Width(w), GUILayout.Height(h));
            //Get the event
            EventType eventType = Event.current.type;
            //Create the bool that will decide what the function returns
            bool isAccepted = false;

            //Add a check to allow me to drag things onto nodes without dropping this stuff on
            foreach (Node node in nodes)
            {
                if (node.rect.Contains(Event.current.mousePosition)) { return null; }
            }

            //If drag happened
            if (eventType == EventType.DragUpdated || eventType == EventType.DragPerform)
            {
                //Visual aid, turns cursor into standard 'copy' icon.
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                //If this is the release of the drag, proceed with it using DragAndDrop Unity base functionality
                if (eventType == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    isAccepted = true;
                }
                Event.current.Use();
            }

            return isAccepted ? DragAndDrop.objectReferences : null;
        }
    }
}

#endif