#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using KiwiKaleidoscope;
using KiwiKaleidoscope.Dialogue;
using KiwiKaleidoscope.Narrative;

using System.IO;

namespace NodeEditor
{
    public class QuestEditor : NodeEditorBase
    {
        private QuestData currentLoadedData;
        private string loadedWindowName;

        private float nodeBaseWidth = 300f;
        private float nodeBaseHeight = 100f;
        //Scaler
        private static float scaleFactor = 1f;
        public static float ScaleFactor
        {
            get
            {
                return scaleFactor;
            }
        }

        bool dragging = false;
        Rect focusMenuRect;

        Vector2 menuMousePos;

        [MenuItem("Window/Quest Editor")]
        public static void OpenWindow()
        {
            QuestEditor window = GetWindow<QuestEditor>();
            window.titleContent = new GUIContent("Quest Editor");
        }

        private void OnEnable()
        {
            nodes = new List<Node>();
            connections = new List<Connection>();
            Load(Application.dataPath + "/Editor/NodeEditor/WindowData/questEditorLastWindow.asset", false);
        }

        private void OnDestroy()
        {
            SaveCurrent();
        }

        #region NodeRemoval
        public static void RemoveNode(Node node)
        {
            //Safety check.
            if (node == null) return;
            //Get instance and run the logic.
            QuestEditor instance = GetWindow<QuestEditor>();
            instance.RemoveActiveNode(node);
        }

        private void RemoveActiveNode(Node node)
        {
            //Get a list of connections affected by the node removal.
            List<Connection> changedConnections = new List<Connection>(connections.FindAll((c) => c.start.node == node || c.end.node == node));
            //Update the affected nodes without updating the base SO.
            foreach (Connection c in changedConnections)
            {
                c.start.node.ConnectionRemoved(c, false);
                c.end.node.ConnectionRemoved(c, false);
                //Remove the connection.
                connections.Remove(c);
            }
            //Finally, remove the node.
            nodes.Remove(node);
        }

        //Global method that attempts to clean up connections broken after changing the amount of stages in a quest.
        public static void CheckStageConnections(ConnectionPoint removedPoint)
        {
            //Safety check.
            if (removedPoint == null) return;
            //Get instance and run the logic.
            QuestEditor instance = GetWindow<QuestEditor>();
            instance.UpdateStageConnections(removedPoint);
        }

        //Local method that goes and cleans up existing connections.
        private void UpdateStageConnections(ConnectionPoint removedPoint)
        {
            List<Connection> changedConnections = new List<Connection>(connections.FindAll((c) => c.start == removedPoint));
            //Update the affected nodes without updating the base SO.
            foreach (Connection c in changedConnections)
            {
                c.start.node.ConnectionRemoved(c, false);
                c.end.node.ConnectionRemoved(c, true);
                //Remove the connection.
                connections.Remove(c);
            }
        }
        #endregion

        private void OnGUI()
        {
            //Check drag and drop
            CheckDragAndDrop(Event.current);

            //Draw grid
            DrawGrid(20, 0.25f);
            DrawGrid(100, 0.5f);

            foreach (Node n in nodes) n.DrawNode();
            if (connections != null)
            {
                for (int i = 0; i < connections.Count; i++)
                {
                    connections[i].DrawConnection();
                }
            }

            ProcessNodeEvents(Event.current);
            ProcessEvents(Event.current);

            DrawConnectionLine(Event.current);

            //Draw tool bar
            DrawToolBar();

            if (GUI.changed) Repaint();
        }

        private void ProcessEvents(Event e)
        {
            //Null drag so it doesn't compound.
            drag = Vector2.zero;
            //Set the grid space.
            Rect gridSpace = new Rect(0, toolbarHeight, position.width, position.height);

            bool clickedOnNode = false;
            //Check that the click originated outside of node windows (as they will have their own style).
            if (nodes != null && nodes.Count > 0)
            {
                foreach (Node n in nodes)
                {
                    if (n.rect.Contains(e.mousePosition)) clickedOnNode = true; break;
                }
            }

            switch (e.type)
            {
                case EventType.MouseDrag:
                    //If drag is within the grid space (ie not on the tool bar or anything).
                    if (e.button == 0 && gridSpace.Contains(e.mousePosition) && dragging)
                    {
                        //Drag
                        OnDrag(e.delta);
                        //Use the event.
                        e.Use();
                        GUI.changed = true;
                    }
                    break;
                case EventType.MouseUp:
                    dragging = false;
                    break;
                case EventType.MouseDown:
                    //Start draggin on left click.
                    if (e.button == 0 && gridSpace.Contains(e.mousePosition) && !clickedOnNode)
                    {
                        dragging = true;
                        GUI.changed = true;
                    }
                    if(e.button == 1 && gridSpace.Contains(e.mousePosition) && !clickedOnNode)
                    {
                        menuMousePos = e.mousePosition;
                        GenericMenu menu = new GenericMenu();
                        //menu.AddItem(new GUIContent("Save"), false, SaveCurrent);
                        //menu.AddItem(new GUIContent("Save as..."), false, SaveAs);
                        //menu.AddItem(new GUIContent("Load quest"), false, LoadQuest);
                        menu.AddSeparator("");
                        menu.AddItem(new GUIContent("Create a new Quest"), false, MenuCallback, "quest");
                        menu.AddItem(new GUIContent("Create a new Stage"), false, MenuCallback, "stage");
                        menu.ShowAsContext();
                        e.Use();
                    }
                    break;
            }
        }

        private void DrawToolBar()
        {
            //Setup the rect for the tool bar.
            toolbarRect = new Rect(0, 0, position.width, toolbarHeight);

            //Begin area and the horizontal layout.
            GUILayout.BeginArea(toolbarRect, EditorStyles.toolbar);
            GUILayout.BeginHorizontal();

            //Name of the current loaded tree.

            GUILayout.Label(loadedWindowName, EditorStyles.label);

            //Show the grid offset.
            GUILayout.Label(new GUIContent(gridOffset.ToString()), EditorStyles.label);

            if (GUILayout.Button("Reset offset", EditorStyles.toolbarButton, GUILayout.Width(80f)))
            {
                foreach (Node n in nodes)
                {
                    n.rect.position -= gridOffset;
                }
                gridOffset = Vector2.zero;
            }


            //Push the elements to the right.
            GUILayout.FlexibleSpace();

            //A way to recenter the editor view on a node.
            bool focus = EditorGUILayout.DropdownButton(new GUIContent("Focus", "Select a node to center the view on"), FocusType.Passive, EditorStyles.toolbarDropDown);
            //Rect for the drop down has to be grabbed during a repaint even or else it will show-up in a wrong spot.
            if (Event.current.type == EventType.Repaint)
            {
                focusMenuRect = GUILayoutUtility.GetLastRect();
            }
            //If button was pressed, open the drop down menu with the nodes.
            if (focus)
            {
                GenericMenu menu = new GenericMenu();
                if (nodes.Count == 0)
                {
                    menu.AddDisabledItem(new GUIContent("No nodes to center on."));
                }
                else
                {
                    for (int i = 0; i < nodes.Count; i++)
                    {
                        menu.AddItem(new GUIContent(nodes[i].nodeTitle.ToString()), false, FocusNode, i);
                    }
                }
                menu.DropDown(focusMenuRect);
            }

            //Label the scale slider.
            GUILayout.Label(new GUIContent("Scale"), EditorStyles.label);
            //Set the scale factor to be controllable through the slider.
            scaleFactor = GUILayout.HorizontalSlider(scaleFactor, 0.5f, 1.5f, GUILayout.Width(100f));

            //Output the current scale so users have an idea of what it is.
            GUILayout.Label(System.Math.Round(scaleFactor, 2).ToString() + "x", EditorStyles.label, GUILayout.Width(40f));

            //Allow for resetting of the scale factor to normal.
            if (GUILayout.Button("Reset", EditorStyles.toolbarButton, GUILayout.Width(50f)))
            {
                scaleFactor = 1f;
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void CheckDragAndDrop(Event e)
        {
            //Get objects that were dragged into the window
            object[] draggedObjects = DropZone("Dragged", 1, 1);
            if (draggedObjects != null)
            {
                //Implement a position offset in case multiple objects were dragged in.
                Vector2 posOffset = Vector2.zero;
                bool newNodeAdded = false;
                foreach (object newObj in draggedObjects)
                {
                    newNodeAdded = false;
                    if (newObj.GetType() == typeof(Quest))
                    {
                        Quest q = newObj as Quest;
                        nodes.Add(new QuestNode(e.mousePosition + posOffset, nodeBaseWidth, nodeBaseHeight, q, OnClickOutPoint));
                        newNodeAdded = true;
                    }
                    if(newObj.GetType() == typeof(QuestStage))
                    {
                        QuestStage qs = newObj as QuestStage;
                        nodes.Add(new QuestStageNode(e.mousePosition + posOffset, nodeBaseWidth * 0.66f, nodeBaseHeight, qs, OnClickInPoint, OnClickOutPoint));
                        newNodeAdded = true;
                    }
                    if (newObj.GetType().IsSubclassOf(typeof(NarrativeTrigger)))
                    {
                        NarrativeTrigger t = newObj as NarrativeTrigger;
                        nodes.Add(new TriggerNode(e.mousePosition + posOffset, nodeBaseWidth * 0.66f, nodeBaseHeight, t, OnClickInPoint));
                        newNodeAdded = true;
                    }
                    if (newObj.GetType().IsSubclassOf(typeof(Condition)))
                    {
                        Condition c = newObj as Condition;
                        nodes.Add(new QuestConditionNode(e.mousePosition + posOffset, nodeBaseWidth * 0.66f, nodeBaseHeight, c, OnClickInPoint));
                        newNodeAdded = true;
                    }
                    //If new node is added shift the offset.
                    if (newNodeAdded)
                    {
                        posOffset.x += 250;
                        if (posOffset.x >= 750)
                        {
                            posOffset.y += 200;
                            posOffset.x = 0;
                        }
                    }
                }

                GUI.changed = true;
            }
        }

        //CALLBACKS FOR THE MENU

        private void MenuCallback(object clb)
        {
            string context = clb.ToString();
            string path;

            switch (context)
            {
                case "quest":
                    //Get path through a menu
                    path = EditorUtility.SaveFilePanel("Save new Quest as..", Application.dataPath + "/Resources/Quests", "", "asset");
                    //Safety check incase users Cancels out of the panel.
                    if (path.Length == 0) break;
                    //Cut the string so its Assets relative
                    path = path.Substring(path.LastIndexOf("Assets"));
                    Quest newQuest = CreateInstance<Quest>();
                    AssetDatabase.CreateAsset(newQuest, path);
                    newQuest = AssetDatabase.LoadAssetAtPath<Quest>(path);
                    if (newQuest != null) LoadNodeFromSO(newQuest);
                    break;
                case "stage":
                    //Get path through a menu
                    path = EditorUtility.SaveFilePanel("Save new Quest Stage as..", Application.dataPath + "/Resources/Quests/Quest Stages", "", "asset");
                    //Safety check incase users Cancels out of the panel.
                    if (path.Length == 0) break;
                    //Cut the string so its Assets relative
                    path = path.Substring(path.LastIndexOf("Assets"));
                    QuestStage newQStage = CreateInstance<QuestStage>();
                    AssetDatabase.CreateAsset(newQStage, path);
                    newQStage = AssetDatabase.LoadAssetAtPath<QuestStage>(path);
                    if (newQStage != null) LoadNodeFromSO(newQStage);
                    break;
            }
        }

        #region Saving
        private void SaveCurrent()
        {
            SaveQuest(currentLoadedData?.path, scaleFactor, gridOffset, nodes, connections);
        }

        private void SaveAs()
        {
            string path = EditorUtility.SaveFilePanelInProject("Save quest", "New quest tree", "asset", "Create new quest");
            if (path.Length > 0)
            {
                path = path.Substring(path.LastIndexOf("Assets/") + 6);
                SaveQuest(path, scaleFactor, gridOffset, nodes, connections);
            }
        }

        private void SaveQuest(string path, float scaleFactor, Vector2 gridOffset, List<Node> nodes, List<Connection> connections)
        {
            string saveLocation = Application.dataPath + "/Editor/NodeEditor/WindowData/questEditorLastWindow.asset";

            if (path != null && path.Length > 0) saveLocation = Application.dataPath + path;
            //Initialize required data.
            string p = (path == null) ? "/Editor/NodeEditor/WindowData/questEditorLastWindow.asset" : path;
            List<NodeData> nodeData = new List<NodeData>();
            if (nodes != null && nodes.Count > 0)
            {
                foreach (Node node in nodes)
                {
                    if (node != null)
                    {
                        NodeData nData = new NodeData(node);
                        nodeData.Add(nData);
                    }
                }
            }
            List<ConnectionData> connectionData = new List<ConnectionData>();
            if (connections != null && connections.Count > 0)
            {
                foreach (Connection connection in connections)
                {
                    if (connection != null && connection.start != null && connection.end != null)
                    {
                        ConnectionData cData = new ConnectionData(connection);
                        connectionData.Add(cData);
                    }
                }
            }

            if (!File.Exists(saveLocation))
            {
                QuestData questData = CreateInstance<QuestData>();
                AssetDatabase.CreateAsset(questData, "Assets" + p);
            }
            QuestData q = AssetDatabase.LoadAssetAtPath<QuestData>("Assets" + p);
            //Safety check
            if (q == null)
            {
                return;
            }
            q.SetData(p, scaleFactor, gridOffset, nodeData, connectionData);

            EditorUtility.SetDirty(q);
            //AssetDatabase.SaveAssets();

            //Set up current loaded
            currentLoadedData = q;
            if (currentLoadedData.path.Length > 0)
            {
                loadedWindowName = currentLoadedData.path.Substring(currentLoadedData.path.LastIndexOf("/") + 1);
                loadedWindowName = loadedWindowName.Substring(0, loadedWindowName.Length - 6);
            }
        }
        #endregion

        #region Loading

        private void LoadNodeFromSO(ScriptableObject so)
        {
            //Initialize nodes in case its null (it never should be, but never hurts to check).
            if (nodes == null) nodes = new List<Node>();

            if (so.GetType() == typeof(Quest))
            {
                QuestNode newQuestNode = new QuestNode(menuMousePos, nodeBaseWidth, nodeBaseHeight, so as Quest, OnClickOutPoint);
                nodes.Add(newQuestNode);
            }
            else if (so.GetType() == typeof(QuestStage))
            {
                QuestStageNode newQStageNode = new QuestStageNode(menuMousePos, nodeBaseWidth, nodeBaseHeight, so as QuestStage, OnClickInPoint, OnClickOutPoint);
                nodes.Add(newQStageNode);
            }
            else
            {
                Debug.LogError("Provided file is not supported by this editor.");
            }
        }

        private void LoadQuest()
        {
            string path = EditorUtility.OpenFilePanel("Load a quest", Application.dataPath + "/Editor/NodeEditor/WindowData", "asset");
            Load(path);
        }

        private void Load(string path, bool saveCurrent = true)
        {
            if (path.Length > 0)
            {
                //Save current tree
                if(saveCurrent) SaveCurrent();
                //Clear the nodes.
                nodes.Clear();
                connections.Clear();
                //Get the data at the path.
                QuestData loadedData = AssetDatabase.LoadAssetAtPath<QuestData>(path.Substring(path.LastIndexOf("Assets")));
                if (loadedData == null)
                {
                    Debug.Log("Couldn't find loaded data for the Quest Editor");
                    return; //SAFETY CHECK
                }
                scaleFactor = loadedData.scaleFactor;
                scaleFactor = Mathf.Clamp(scaleFactor, 0.5f, 1.5f);
                gridOffset = loadedData.gridOffset;
                //Reconstruct the nodes.
                if (loadedData.nodes != null && loadedData.nodes.Count > 0)
                {
                    //Get all the quests, triggers and conditions.
                    List<Quest> quests = new List<Quest>(Resources.LoadAll<Quest>("Quests"));
                    List<QuestStage> questStages = new List<QuestStage>(Resources.LoadAll<QuestStage>("Quests/Quest Stages"));
                    List<NarrativeTrigger> triggers = new List<NarrativeTrigger>(Resources.LoadAll<NarrativeTrigger>("Quests/Quest Triggers"));
                    List<Condition> conditions = new List<Condition>(Resources.LoadAll<Condition>("Quests"));
                    //Go through all the node data and reconstruct nodes.
                    foreach(NodeData nData in loadedData.nodes)
                    {
                        switch(nData.id[nData.id.Length - 1])
                        {
                            case ('q'):
                                Quest quest = quests.Find((p) => p.id == nData.id);
                                if (quest != null) nodes.Add(new QuestNode(nData.pos, nodeBaseWidth, nodeBaseHeight, quest, OnClickOutPoint));
                                break;
                            case ('s'):
                                QuestStage questStage = questStages.Find((p) => p.id == nData.id);
                                if (questStage != null) nodes.Add(new QuestStageNode(nData.pos, nodeBaseWidth * 0.66f, nodeBaseHeight, questStage, OnClickInPoint, OnClickOutPoint));
                                break;
                            case ('c'):
                                Condition condition = conditions.Find((p) => p.id == nData.id);
                                if (condition != null) nodes.Add(new QuestConditionNode(nData.pos, nodeBaseWidth * 0.66f, nodeBaseHeight, condition, OnClickInPoint));
                                break;
                            case ('t'):
                                NarrativeTrigger trigger = triggers.Find((p) => p.id == nData.id);
                                if (trigger != null) nodes.Add(new TriggerNode(nData.pos, nodeBaseWidth * 0.66f, nodeBaseHeight, trigger, OnClickInPoint));
                                break;
                        }
                    }
                }
                //Reconstruct the connections
                if (loadedData.connections != null && loadedData.connections.Count > 0)
                {
                    ConnectionPoint startPoint = null;
                    ConnectionPoint endPoint = null;
                    //Go through all the connection data and create connections based on start and end node ids.
                    foreach (ConnectionData cData in loadedData.connections)
                    {
                        if(ContainsNodeID(cData.startID) && ContainsNodeID(cData.endID))
                        {
                            //Cashe start and end nodes of the connection.
                            Node start = nodes.Find((n) => n.GetNodeID() == cData.startID);
                            Node end = nodes.Find((n) => n.GetNodeID() == cData.endID);
                            //Special case with the quest node since those things are procedurally generated.
                            if(start.GetType() == typeof(QuestNode))
                            {
                                Quest quest = start.GetValue() as Quest;
                                QuestStage stage = end.GetValue() as QuestStage;
                                for(int i = 0; i < quest.stageAmount; i++)
                                {
                                    if(quest.stages[i] == stage)
                                    {
                                        startPoint = (start as QuestNode).outPoints[i];
                                    }
                                }
                            }
                            else
                            {
                                //Otherwise set the start point.
                                startPoint = start.GetConnectionPoints().outPoint;
                            }
                            //Set the end point.
                            endPoint = end.GetConnectionPoints().inPoint;
                            //Make the connection
                            Connection newCon = new Connection(startPoint, endPoint, RemoveConnection);
                            if (!connections.Contains(newCon)) connections.Add(newCon);
                            //Cause the node child/parent lists to update without updating the base SO.
                            start.ConnectionAdded(newCon, false);
                            end.ConnectionAdded(newCon, false);
                        }
                    }
                }
                currentLoadedData = loadedData;
                if(currentLoadedData.path.Length > 0)
                {
                    loadedWindowName = currentLoadedData.path.Substring(currentLoadedData.path.LastIndexOf("/") + 1);
                    loadedWindowName = loadedWindowName.Substring(0, loadedWindowName.Length - 6);
                }
            }
        }

        #endregion

        private bool ContainsNodeID(string id)
        {
            if (nodes.Find((n) => n.GetNodeID() == id) != null) return true;
            return false;
        }
    }
}

#endif