#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using KiwiKaleidoscope;

namespace NodeEditor
{
    public class QuestNode : Node
    {
        public Quest quest;

        private float scaledWidth;
        private float contentHeight = 50f;
        private float scaledContent;

        private string content;


        Vector2 scrollView;

        //GUIStyles
        GUIStyle boxDark;
        GUIStyle boxGray;

        //Quest outPoints will be dynamically generated so save the action.
        private System.Action<ConnectionPoint> OnClickOutPoint;

        public List<ConnectionPoint> outPoints = new List<ConnectionPoint>();

        public QuestNode(Vector2 pos, float width, float height, Quest quest, System.Action<ConnectionPoint> OnClickOutPoint)
        {
            rect = new Rect(pos.x, pos.y, width, height);
            nodeWidth = width;
            nodeHeight = height;
            //Get styles
            boxDark = NodeEditorStyles.BoxDark;
            boxGray = NodeEditorStyles.BoxGray;
            //Set the variables.
            this.quest = quest;
            nodeTitle = quest.name;
            this.OnClickOutPoint = OnClickOutPoint;

            //Add existing connectors.
            for(int i = 0;i< quest.stageAmount; i++)
            {
                outPoints.Add(new ConnectionPoint(this, ConnectionType.Out, OnClickOutPoint));
            }

            nodeType = "Quest";
        }

        public override void DrawNode()
        {
            if (quest == null) RemoveNode(this);

            CalculateScaling();

            //Make a group for this node. This translates all the future positions into local space based on the Rect of the node.
            GUI.BeginGroup(rect);
            //Make a bar for the node title.
            GUI.Box(new Rect(0, 0, scaledWidth, titleBarHeight), nodeTitle, boxDark);
            //Make a bar for the node type.
            GUI.Box(new Rect(0, titleBarHeight, scaledWidth, typeBarHeight), nodeType + ": id" + quest.id, boxGray);

            GUI.Box(new Rect(0, titleBarHeight + typeBarHeight, scaledWidth, typeBarHeight), "");
            string qName = quest.questName;
            qName = EditorGUI.TextField(new Rect(0, titleBarHeight + typeBarHeight, scaledWidth, typeBarHeight), quest.questName, EditorStyles.textField);
            quest.questName = qName;

            //Make a box for quest description.
            GUI.Box(new Rect(0, titleBarHeight + typeBarHeight * 2, scaledWidth, scaledContent), "");
            content = quest.questDescription;
            content = EditorGUI.TextArea(new Rect(0, titleBarHeight + typeBarHeight * 2, scaledWidth, scaledContent), content, EditorStyles.textArea);
            quest.questDescription = content;

            //Make a bar for the stage amount.
            GUI.Box(new Rect(0, titleBarHeight + typeBarHeight * 2 + scaledContent, scaledWidth, titleBarHeight), "", boxGray);
            EditorGUI.PrefixLabel(new Rect(0, titleBarHeight + typeBarHeight * 2 + scaledContent, scaledWidth, titleBarHeight), new GUIContent("Stage Amount"), EditorStyles.whiteLabel);
            int stageAmount = quest.stageAmount;
            stageAmount = EditorGUI.IntField(new Rect(scaledWidth * 0.75f, titleBarHeight + typeBarHeight * 2 + scaledContent, scaledWidth, titleBarHeight), "", stageAmount);
            quest.stageAmount = stageAmount;



            //In case stage amount has changed, resize the stage list.
            if (stageAmount < quest.stages.Count)
            {
                //Remove space
                quest.stages.RemoveRange(stageAmount, quest.stages.Count - stageAmount);
            }
            else if (stageAmount > quest.stages.Count)
            {
                //Add space
                for (int i = quest.stages.Count; i < stageAmount; i++)
                {
                    quest.stages.Add(null);
                }
            }
            //Remove some left over connectors as well
            if (stageAmount < outPoints.Count)
            {
                //Try and clean up the connections from those
                for(int i = stageAmount;i < outPoints.Count; i++)
                {
                    QuestEditor.CheckStageConnections(outPoints[i]);
                }
                outPoints.RemoveRange(stageAmount, outPoints.Count - stageAmount);
            }

            GUI.Box(new Rect(0, titleBarHeight * 2 + typeBarHeight * 2 + scaledContent, scaledWidth, stageAmount * 30), "");

            //Display the stages
            float offset = 0;
            for (int i = 0; i < stageAmount; i++)
            {
                EditorGUI.ObjectField(new Rect(0, titleBarHeight * 2 + typeBarHeight * 2 + scaledContent + offset, scaledWidth, typeBarHeight), quest.stages[i], typeof(QuestStage), false);
                offset += 30f;
            }

            //Make a box for the rewards header
            GUI.Box(new Rect(0, titleBarHeight * 2 + typeBarHeight * 2 + scaledContent + offset, scaledWidth, titleBarHeight), "Quest Rewards", boxGray);
            //Outline the reward panel that will be used here.
            Rect rewardPanel = new Rect(0, titleBarHeight * 3 + typeBarHeight * 2 + scaledContent + offset, scaledWidth, 200);
            //Make a box for the background.
            GUI.Box(rewardPanel, "");

            //Begin auto-layed out area.
            GUILayout.BeginArea(rewardPanel);
            //Add scroll view
            scrollView = GUILayout.BeginScrollView(scrollView);
            //Int field for the money reward.
            int moneyReward = quest.moneyReward;
            moneyReward = EditorGUILayout.IntField("Money Reward", moneyReward);
            quest.moneyReward = moneyReward;
            //Get the rewards list as a property.
            SerializedObject sObj = new SerializedObject(quest);

            SerializedProperty dialogue = sObj.FindProperty("dialogueToTrigger");
            EditorGUILayout.PropertyField(dialogue, new GUIContent("Dialogue"), true);

            SerializedProperty sProp = sObj.FindProperty("rewards");
            EditorGUILayout.PropertyField(sProp, new GUIContent("Rewards"), true);
            //Update the base.
            sObj.ApplyModifiedProperties();

            EditorUtility.SetDirty(quest);

            //End scrollview
            GUILayout.EndScrollView();
            //End the area.
            GUILayout.EndArea();

            //Modify the rect height
            rect.height = titleBarHeight * 3 + typeBarHeight * 2 + scaledContent + stageAmount * 30 + 200;

            GUI.EndGroup();

            // Draw connectors.Has to be outside the GUI.Group or else it messes with them.
            offset = 0;
            for (int i = 0; i < stageAmount; i++)
            {
                if (outPoints.Count < stageAmount)
                {
                    outPoints.Add(new ConnectionPoint(this, ConnectionType.Out, OnClickOutPoint));
                }
                outPoints[i].DrawConnector(rect.y + titleBarHeight * 2 + typeBarHeight + scaledContent + offset);
                //Increase the offset.
                offset += 30;
            }
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
            scaledContent = contentHeight * QuestEditor.ScaleFactor;
            rect.width = scaledWidth;
        }

        public override bool ConnectionAdded(Connection connection, bool updateBase = true)
        {
            //There is only a bunch of out points and a single allowed type, so its really simple
            if (connection.end.node.GetType() != typeof(QuestStageNode))
            {
                Debug.LogWarning("Cannot link Quest nodes to any non-QuestStage nodes.");
                return false;
            }
            if(updateBase) UpdateSO(connection, true);
            //Update child nodes.
            if (childNodes == null) childNodes = new List<Node>();
            childNodes.Add(connection.end.node);
            return true;
        }

        public override void ConnectionRemoved(Connection connection, bool updateBase = true)
        {
            if (updateBase) UpdateSO(connection, false);
            if(childNodes != null) childNodes.Remove(connection.end.node);
        }

        private void UpdateSO(Connection c, bool connectionAdded)
        {
            if (connectionAdded)
            {
                //Get which connection point is the start
                for(int i = 0;i< outPoints.Count || i < quest.stageAmount; i++)
                {
                    if(c.start == outPoints[i])
                    {
                        //Set that stage
                        quest.stages[i] = c.end.node.GetValue() as QuestStage;
                    }
                }
            }
            else
            {
                //Get which connection point is the start
                for (int i = 0; i < outPoints.Count || i < quest.stageAmount; i++)
                {
                    if (c.start == outPoints[i])
                    {
                        //Null that stage
                        quest.stages[i] = null;
                    }
                }
            }
        }

        public override ConnectionPoints GetConnectionPoints()
        {
            return null;
        }

        public override string GetNodeID()
        {
            return quest.id;
        }

        public override ScriptableObject GetValue()
        {
            return quest;
        }
    }
}
#endif