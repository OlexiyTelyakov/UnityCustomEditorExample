#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using KiwiKaleidoscope.Dialogue;

namespace NodeEditor
{
    public class QuestConditionNode : Node
    {
        public Condition condition;

        private float conditionFieldHeight = 30f;
        private float logicHeight = 150f;

        //Scaled values used in drawing the node after a scale value has been applied.
        private float scaledWidth;
        private bool showLogic;

        private Vector2 logicScrollView;

        //GUI style
        GUIStyle conditionYellow;
        GUIStyle boxDark;
        GUIStyle boxGray;

        ConnectionPoint inPoint;

        public QuestConditionNode(Vector2 pos, float width, float height, Condition condition, System.Action<ConnectionPoint> OnClickInPoint)
        {
            rect = new Rect(pos.x, pos.y, width, height);
            nodeWidth = width;
            nodeHeight = height;
            //Get the styles
            boxDark = NodeEditorStyles.BoxDark;
            boxGray = NodeEditorStyles.BoxGray;
            conditionYellow = NodeEditorStyles.ConditionYellow;
            //Set the variables
            this.condition = condition;
            nodeTitle = condition.name;
            nodeType = "Condition";

            //Setup the inPoint
            inPoint = new ConnectionPoint(this, ConnectionType.In, OnClickInPoint);
        }

        public override void DrawNode()
        {
            //If SO is ever null, meaning something happened to it, remove node.
            if (condition == null) RemoveNode(this);

            CalculateScaling();
            //Draw the connectors
            float yPos = rect.y + rect.height / 2f - inPoint.rect.height / 2f;
            inPoint.DrawConnector(yPos);

            //Make a group for this node. This translates all the future positions into local space based on the Rect of the node.
            GUI.BeginGroup(rect);
            //Make a bar for the node title.
            GUI.Box(new Rect(0, 0, scaledWidth, titleBarHeight), nodeTitle, conditionYellow);
            //Make a bar for the node type.
            GUI.Box(new Rect(0, titleBarHeight, scaledWidth, typeBarHeight), nodeType + ": id" + condition.id, boxGray);

            //TODO: Have a way to draw every condition type as opposed to just dumping a value!

            float additionalHeight = 0;

            //FoldoutMenu
            GUI.Box(new Rect(0, titleBarHeight + typeBarHeight, scaledWidth, typeBarHeight), "");
            showLogic = EditorGUI.Foldout(new Rect(0, titleBarHeight + typeBarHeight, scaledWidth, typeBarHeight), showLogic, new GUIContent("Show Condition Data"), true);

            if (showLogic)
            {
                Rect conditionTooltipRect = new Rect(0, titleBarHeight + typeBarHeight * 2, scaledWidth, logicHeight);
                additionalHeight += logicHeight;
                GUI.Box(conditionTooltipRect, "");
                //Autolayout
                GUILayout.BeginArea(conditionTooltipRect);
                logicScrollView = GUILayout.BeginScrollView(logicScrollView);
                //Display the list
                SerializedObject sobj = new SerializedObject(condition);

                //RESOURCE CONDITION
                if (condition.GetType() == typeof(ResourceCondition))
                {
                    SerializedProperty reqItems = sobj.FindProperty("requiredItems");
                    EditorGUILayout.PropertyField(reqItems, new GUIContent("Required Resources"), true);

                    SerializedProperty reqMoney = sobj.FindProperty("requiredCurrency");
                    EditorGUILayout.PropertyField(reqMoney, new GUIContent("Required Money"));
                    sobj.ApplyModifiedProperties();
                }
                //QUEST CONDITION
                if (condition.GetType() == typeof(QuestCondition))
                {
                    SerializedProperty quest = sobj.FindProperty("quest");
                    EditorGUILayout.PropertyField(quest, new GUIContent("Quest"));
                    SerializedProperty isActive = sobj.FindProperty("isActive");
                    EditorGUILayout.PropertyField(isActive, new GUIContent("Is Active"));
                    SerializedProperty isCompleted = sobj.FindProperty("isCompleted");
                    EditorGUILayout.PropertyField(isCompleted, new GUIContent("Is completed"));
                    SerializedProperty minStage = sobj.FindProperty("reqStageMin");
                    EditorGUILayout.PropertyField(minStage, new GUIContent("Stage range min"));
                    SerializedProperty maxStage = sobj.FindProperty("reqStageMax");
                    EditorGUILayout.PropertyField(maxStage, new GUIContent("Stage range max"));
                    sobj.ApplyModifiedProperties();
                }
                //TIME CONDITION
                if (condition.GetType() == typeof(TimeCondition))
                {
                    SerializedProperty checkTime = sobj.FindProperty("checkTime");
                    EditorGUILayout.PropertyField(checkTime, new GUIContent("Check Time"));
                    SerializedProperty timeOfDayMin = sobj.FindProperty("timeOfDayMin");
                    EditorGUILayout.PropertyField(timeOfDayMin, new GUIContent("Time min"));
                    SerializedProperty timeOfDayMax = sobj.FindProperty("timeOfDayMax");
                    EditorGUILayout.PropertyField(timeOfDayMax, new GUIContent("Time max"));

                    SerializedProperty checkDay = sobj.FindProperty("checkDay");
                    EditorGUILayout.PropertyField(checkDay, new GUIContent("Check Day"));
                    SerializedProperty dayMin = sobj.FindProperty("dayMin");
                    EditorGUILayout.PropertyField(dayMin, new GUIContent("Day min"));
                    SerializedProperty dayMax = sobj.FindProperty("dayMax");
                    EditorGUILayout.PropertyField(dayMax, new GUIContent("Day max"));
                    sobj.ApplyModifiedProperties();
                }

                //End autolayout
                GUILayout.EndScrollView();
                GUILayout.EndArea();
            }

            //Notifications
            if (condition.GetType() == typeof(QuestCondition) && (condition as QuestCondition).quest == null)
            {
                EditorGUI.HelpBox(new Rect(0, titleBarHeight + typeBarHeight * 2 + additionalHeight, scaledWidth, conditionFieldHeight),
                    "No Quest attached to the condition. Will always return false!", MessageType.Warning);
                additionalHeight += conditionFieldHeight;
            }
            if (condition.GetType() == typeof(TimeCondition) && !(condition as TimeCondition).checkDay && !(condition as TimeCondition).checkTime)
            {
                EditorGUI.HelpBox(new Rect(0, titleBarHeight + typeBarHeight * 2 + additionalHeight, scaledWidth, conditionFieldHeight),
                    "Not checking for either time or day count. Will always return false.", MessageType.Warning);
                additionalHeight += conditionFieldHeight;
            }

            rect.height = titleBarHeight + typeBarHeight * 2 + additionalHeight;

            EditorUtility.SetDirty(condition);

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

        public override bool ConnectionAdded(Connection connection, bool updateBase = true)
        {
            if (connection.start.node.GetType() != typeof(QuestStageNode))
            {
                Debug.LogWarning("Condition nodes can only be linked to QuestStage nodes");
                return false;
            }
            return true;
        }

        public override void ConnectionRemoved(Connection connection, bool updateBase = true)
        {
            //There is nothing to update.
            return;
        }

        public override ConnectionPoints GetConnectionPoints()
        {
            return new ConnectionPoints(inPoint, null);
        }

        public override string GetNodeID()
        {
            return condition.id;
        }

        public override ScriptableObject GetValue()
        {
            return condition;
        }
    }
}
#endif