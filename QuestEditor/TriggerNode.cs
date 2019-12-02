#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using KiwiKaleidoscope.Narrative;

namespace NodeEditor
{
    public class TriggerNode : Node
    {
        public NarrativeTrigger trigger;

        private float scaledWidth;

        private Vector2 scrollView;

        //GUIStyles
        GUIStyle boxDark;
        GUIStyle boxGray;
        GUIStyle triggerBlue;

        //Connector
        ConnectionPoint inPoint;

        public TriggerNode(Vector2 pos, float width, float height, NarrativeTrigger trigger, System.Action<ConnectionPoint> OnClickInPoint)
        {
            rect = new Rect(pos.x, pos.y, width, height);
            nodeWidth = width;
            nodeHeight = height;
            //Get the styles
            boxDark = NodeEditorStyles.BoxDark;
            boxGray = NodeEditorStyles.BoxGray;
            triggerBlue = NodeEditorStyles.TriggerBlue;
            //Set the variables
            this.trigger = trigger;
            nodeTitle = trigger.name;
            nodeType = "Trigger";

            //Setup the inPoint
            inPoint = new ConnectionPoint(this, ConnectionType.In, OnClickInPoint);
        }

        public override void DrawNode()
        {
            if (trigger == null) RemoveNode(this);

            CalculateScaling();
            //Draw the connector
            inPoint.DrawConnector(rect.y + rect.height / 2f - inPoint.rect.height / 2f);

            //Make a group for this node. This translates all the future positions into local space based on the Rect of the node.
            GUI.BeginGroup(rect);
            //Make a bar for the node title.
            GUI.Box(new Rect(0, 0, scaledWidth, titleBarHeight), nodeTitle, triggerBlue);
            //Make a bar for the node type.
            GUI.Box(new Rect(0, titleBarHeight, scaledWidth, typeBarHeight), nodeType + ": id" + trigger.id, boxGray);

            //All the magic depending on the Trigger type.
            float triggerSpecHeight = titleBarHeight + typeBarHeight;
            //ITEM PICK UP
            if(trigger.GetType() == typeof(ItemPickUpTrigger))
            {
                //Update node type
                nodeType = "ItemPickUpTrigger";
                GUI.Box(new Rect(0, titleBarHeight + typeBarHeight, scaledWidth, triggerSpecHeight), "Item", boxGray);
                Item item = (trigger as ItemPickUpTrigger).pickUp;
                item = (Item)EditorGUI.ObjectField(new Rect(0, titleBarHeight * 2 + typeBarHeight, scaledWidth, typeBarHeight), item, typeof(Item), false);
                (trigger as ItemPickUpTrigger).pickUp = item;
                if(item == null)
                {
                    triggerSpecHeight = titleBarHeight + typeBarHeight * 3;
                    GUI.Box(new Rect(0, titleBarHeight * 2 + typeBarHeight * 2, scaledWidth, typeBarHeight * 2), "");
                    EditorGUI.HelpBox(new Rect(0, titleBarHeight * 2 + typeBarHeight * 2, scaledWidth, typeBarHeight * 2), "Null pick up will cause the trigger to fire on ANY pickup", MessageType.Info);
                }
            }
            //ITEM RANGE PICK UP
            if(trigger.GetType() == typeof(ItemRangePickUpTrigger))
            {
                //update node type
                nodeType = "ItemRangePickUpTrigger";
                GUI.Box(new Rect(0, titleBarHeight + typeBarHeight, scaledWidth, triggerSpecHeight), "Item", boxGray);
                Rect itemRange = new Rect(0, titleBarHeight + typeBarHeight + triggerSpecHeight, scaledWidth, 200f);
                GUI.Box(itemRange, "");
                triggerSpecHeight += 200f;

                //Begin auto-layed out area.
                GUILayout.BeginArea(itemRange);
                //Add scroll view
                scrollView = GUILayout.BeginScrollView(scrollView);
                //Get the rewards list as a property.
                SerializedObject sObj = new SerializedObject(trigger);

                SerializedProperty range = sObj.FindProperty("pickUps");
                EditorGUILayout.PropertyField(range, new GUIContent("Item Range"), true);

                //Update the base.
                sObj.ApplyModifiedProperties();

                //End scrollview
                GUILayout.EndScrollView();
                //End the area.
                GUILayout.EndArea();
            }
            //TRAVERSAL
            if(trigger.GetType() == typeof(TraversalTrigger))
            {
                //Update node type
                nodeType = "TraversalTrigger";
                GUI.Box(new Rect(0, titleBarHeight + typeBarHeight, scaledWidth, triggerSpecHeight), "Traverse Name", boxGray);
                string traverse = (trigger as TraversalTrigger).traverserName;
                traverse = EditorGUI.TextArea(new Rect(0, titleBarHeight * 2 + typeBarHeight, scaledWidth, typeBarHeight), traverse, EditorStyles.textArea);
                (trigger as TraversalTrigger).traverserName = traverse;
            }
            //TIME PROGRESS
            if (trigger.GetType() == typeof(TimeProgressTrigger))
            {
                //Update node type
                nodeType = "TimeProgressTrigger";
                triggerSpecHeight = titleBarHeight + typeBarHeight * 3;
                GUI.Box(new Rect(0, titleBarHeight + typeBarHeight, scaledWidth, triggerSpecHeight), "Time and Day", boxGray);
                //Daycount agnostic
                bool agnostic = (trigger as TimeProgressTrigger).dayCountAgnostic;
                agnostic = EditorGUI.Toggle(new Rect(0, titleBarHeight * 2 + typeBarHeight, scaledWidth, typeBarHeight), "DayCount agnostic", agnostic);
                (trigger as TimeProgressTrigger).dayCountAgnostic = agnostic;
                //Daycount to trigger
                int dayCount = (trigger as TimeProgressTrigger).dayToTrigger;
                dayCount = EditorGUI.IntField(new Rect(0, titleBarHeight * 2 + typeBarHeight * 2, scaledWidth, typeBarHeight), "Day to trigger", dayCount);
                (trigger as TimeProgressTrigger).dayToTrigger = dayCount;
                //Time of day to trigger.
                int tod = (trigger as TimeProgressTrigger).timeOfDayTrigger;
                tod = EditorGUI.IntField(new Rect(0, titleBarHeight * 2 + typeBarHeight * 3, scaledWidth, typeBarHeight), "Day to trigger", dayCount);
                tod = Mathf.Clamp(tod, 0, 2400);
                (trigger as TimeProgressTrigger).timeOfDayTrigger = tod;
            }

            rect.height = titleBarHeight + typeBarHeight + triggerSpecHeight;

            EditorUtility.SetDirty(trigger);

            GUI.EndGroup();
        }

        public override bool ConnectionAdded(Connection connection, bool updateBase = true)
        {
            if(connection.start.node.GetType() != typeof(QuestStageNode))
            {
                Debug.LogWarning("Trigger nodes can only be linked to QuestStage nodes");
                return false;
            }
            return true;
        }

        public override void ConnectionRemoved(Connection connection, bool updateBase = true)
        {
            //There is nothing to update.
            return;
        }

        public override bool ProcessEvents(Event e)
        {
            switch (e.type)
            {
                case EventType.MouseDown:
                    //On mouse down, set node as dragged
                    if (e.button == 0 && rect.Contains(e.mousePosition))
                    {
                        isDragged = true;
                        GUI.changed = true;
                    }
                    //On right click, open the context menu.
                    if (e.button == 1 && rect.Contains(e.mousePosition))
                    {
                        //Create menu
                        GenericMenu menu = new GenericMenu();
                        //Add remove node to it
                        menu.AddItem(new GUIContent("Remove Node from Editor"), false, RemoveNode, this);
                        //Display the menu
                        menu.ShowAsContext();
                        //Use up the event.
                        e.Use();
                    }
                    break;
                case EventType.MouseUp: //Mouse up breaks the drag.
                    isDragged = false;
                    break;
                case EventType.MouseDrag:
                    //On left click drag the node and use the event.
                    if (isDragged && e.button == 0)
                    {
                        Drag(e.delta);
                        e.Use();
                        return true;
                    }
                    break;
            }
            return false;
        }

        protected override void RemoveNode(object obj)
        {
            Node node = obj as Node;
            QuestEditor.RemoveNode(node);
        }

        protected override void CalculateScaling()
        {
            scaledWidth = nodeWidth * QuestEditor.ScaleFactor;
            rect.width = scaledWidth;
        }

        public override ConnectionPoints GetConnectionPoints()
        {
            return new ConnectionPoints(inPoint, null);
        }

        public override string GetNodeID()
        {
            return trigger.id;
        }

        public override ScriptableObject GetValue()
        {
            return trigger;
        }
    }
}
#endif