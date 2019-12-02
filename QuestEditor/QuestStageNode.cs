#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using KiwiKaleidoscope;
using KiwiKaleidoscope.Dialogue;
using KiwiKaleidoscope.Narrative;

namespace NodeEditor
{
    public class QuestStageNode : Node
    {
        public QuestStage questStage;

        private float scaledWidth;
        private float contentHeight = 50f;
        private float scaledContent;

        private string content;

        //GUIStyles
        GUIStyle boxDark;
        GUIStyle boxGray;

        //Connectors
        public ConnectionPoint inPoint;
        public ConnectionPoint outPoint;

        public QuestStageNode(Vector2 pos, float width, float height, QuestStage questStage, System.Action<ConnectionPoint> OnClickInPoint, System.Action<ConnectionPoint> OnClickOutPoint)
        {
            rect = new Rect(pos.x, pos.y, width, height);
            nodeWidth = width;
            nodeHeight = height;
            //GetStyles
            boxDark = NodeEditorStyles.BoxDark;
            boxGray = NodeEditorStyles.BoxGray;
            //Set variables
            this.questStage = questStage;
            nodeTitle = questStage.name;
            content = questStage.stageDescription;

            inPoint = new ConnectionPoint(this, ConnectionType.In, OnClickInPoint);
            outPoint = new ConnectionPoint(this, ConnectionType.Out, OnClickOutPoint);

            nodeType = "QuestStage";
        }

        public override void DrawNode()
        {
            if (questStage == null) RemoveNode(this);

            CalculateScaling();
            //Draw the connectors.
            inPoint.DrawConnector(rect.y + rect.height/2 - inPoint.rect.height/2);
            outPoint.DrawConnector(rect.y + rect.height / 2 - inPoint.rect.height / 2);

            //Make a group for this node. This translates all the future positions into local space based on the Rect of the node.
            GUI.BeginGroup(rect);
            //Make a bar for the node title.
            GUI.Box(new Rect(0, 0, scaledWidth, titleBarHeight), nodeTitle, boxDark);
            //Make a bar for the node type.
            GUI.Box(new Rect(0, titleBarHeight, scaledWidth, typeBarHeight), nodeType + ": id" + questStage.id, boxGray);
            //Make a box for the quest.
            string quest = "Quest: " + ((questStage.quest != null) ? questStage.quest.name : "null");
            GUI.Box(new Rect(0, titleBarHeight + typeBarHeight, scaledWidth, titleBarHeight), quest);
            //Make a text area
            content = questStage.stageDescription;
            content = EditorGUI.TextArea(new Rect(0, titleBarHeight * 2 + typeBarHeight, scaledWidth, scaledContent), content, EditorStyles.textArea);
            questStage.stageDescription = content;
            //Make a box for condition.
            GUI.Box(new Rect(0, titleBarHeight * 2 + typeBarHeight + scaledContent, scaledWidth, titleBarHeight + typeBarHeight), "Condition", boxGray);
            Condition condition = questStage.condition;
            condition = (Condition) EditorGUI.ObjectField(new Rect(0, titleBarHeight * 3 + typeBarHeight + scaledContent, scaledWidth, typeBarHeight), condition, typeof(Condition), false);
            questStage.condition = condition;
            //Make a box for a trigger
            GUI.Box(new Rect(0, titleBarHeight * 3 + typeBarHeight * 2 + scaledContent, scaledWidth, titleBarHeight + typeBarHeight), "Trigger", boxGray);
            NarrativeTrigger trigger = questStage.trigger;
            trigger = (NarrativeTrigger)EditorGUI.ObjectField(new Rect(0, titleBarHeight * 4 + typeBarHeight * 2 + scaledContent, scaledWidth, typeBarHeight), trigger, typeof(NarrativeTrigger), false);
            questStage.trigger = trigger;

            rect.height = titleBarHeight * 4 + typeBarHeight * 3 + scaledContent;

            EditorUtility.SetDirty(questStage);

            GUI.EndGroup();
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

        protected override void CalculateScaling()
        {
            scaledWidth = nodeWidth * QuestEditor.ScaleFactor;
            rect.width = scaledWidth;
            scaledContent = contentHeight * QuestEditor.ScaleFactor;
        }

        protected override void RemoveNode(object obj)
        {
            Node node = obj as Node;
            QuestEditor.RemoveNode(node);
        }

        public override bool ConnectionAdded(Connection connection, bool updateBase = true)
        {
            if(connection.start.node == this)
            {
                if (connection.end.node.GetType() != typeof(TriggerNode) && connection.end.node.GetType() != typeof(QuestConditionNode))
                {
                    Debug.LogWarning("Cannot parent QuestStage nodes to nodes other than Trigger and Condition nodes");
                    return false;
                }
                //Update SO base if necessary.
                if (updateBase) UpdateSO(connection, true);
                //Update child list
                if (childNodes == null) childNodes = new List<Node>();
                childNodes.Add(connection.end.node);
            }
            else
            {
                if(connection.start.node.GetType() != typeof(QuestNode))
                {
                    Debug.LogWarning("Cannot child Stage nodes to non-Quest nodes.");
                    return false;
                }
                //Update SO base if necessary.
                if (updateBase) UpdateSO(connection, true);
                //Update parent list.
                if (parentNodes == null) parentNodes = new List<Node>();
                parentNodes.Add(connection.start.node);
            }
            return true;
        }

        public override void ConnectionRemoved(Connection connection, bool updateBase = true)
        {
            if(connection.start.node == this)
            {
                //Update SO base if necessary.
                if (updateBase) UpdateSO(connection, false);
                //Update child list
                if (childNodes != null && childNodes.Contains(connection.end.node)) childNodes.Remove(connection.end.node);
            }
            else
            {
                if(connection.start.node.GetType() == typeof(QuestNode) && updateBase)
                {
                    UpdateSO(connection, false);
                }
                //Update parent list.
                if (parentNodes != null && parentNodes.Contains(connection.start.node)) parentNodes.Remove(connection.start.node);
            }
        }

        private void UpdateSO(Connection c, bool connectionAdded)
        {
            System.Type type = c.end.node.GetType();
            if (connectionAdded)
            {
                //Register conditions and triggers
                if(c.start.node == this)
                {
                    if(type == typeof(TriggerNode))
                    {
                        questStage.trigger = c.end.node.GetValue() as NarrativeTrigger;
                    }
                    if(type == typeof(QuestConditionNode))
                    {
                        questStage.condition = c.end.node.GetValue() as Condition;
                    }
                }
                else
                {
                    questStage.quest = c.start.node.GetValue() as Quest;
                }
            }
            else
            {
                //Clear triggers and conditions
                if (c.start.node == this)
                {
                    if (type == typeof(TriggerNode))
                    {
                        questStage.trigger = null;
                    }
                    if(type == typeof(QuestConditionNode))
                    {
                        questStage.condition = null;
                    }
                }
                else
                {
                    questStage.quest = null;
                }
            }
        }

        public override ConnectionPoints GetConnectionPoints()
        {
            return new ConnectionPoints(inPoint, outPoint);
        }

        public override string GetNodeID()
        {
            return questStage.id;
        }

        public override ScriptableObject GetValue()
        {
            return questStage;
        }
    }
}
#endif