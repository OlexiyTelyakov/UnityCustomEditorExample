#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//UNITY WILL NOT BUILD WITH UNITY EDITOR NAMESPACE IN USE.
using UnityEditor;

namespace NodeEditor
{
    /// <summary>
    /// Class that houses various styles that are used in node editor.
    /// </summary>
    public class NodeEditorStyles : MonoBehaviour
    {
        private static GUIStyle boxDark;
        public static GUIStyle BoxDark
        {
            get
            {
                if(boxDark == null)
                {
                    boxDark = new GUIStyle();
                    boxDark.normal.textColor = Color.white;
                    boxDark.alignment = TextAnchor.UpperCenter;
                    boxDark.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/cnentrybackeven.png") as Texture2D;
                }
                return boxDark;
            }
        }

        private static GUIStyle boxGray;
        public static GUIStyle BoxGray
        {
            get
            {
                if (boxGray == null)
                {
                    boxGray = new GUIStyle();
                    boxGray.normal.textColor = new Color(1,1,1,0.75f);
                    boxGray.alignment = TextAnchor.UpperCenter;
                    boxGray.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/cn entrybackodd.png") as Texture2D;
                }
                return boxGray;
            }
        }

        private static GUIStyle replyGreen;
        public static GUIStyle ReplyGreen
        {
            get
            {
                if(replyGreen == null)
                {
                    replyGreen = new GUIStyle();
                    replyGreen.normal.textColor = Color.black;
                    replyGreen.alignment = TextAnchor.UpperCenter;
                    replyGreen.normal.background = Resources.Load<Texture2D>("EditorStyles/Green"); ;
                }
                return replyGreen;
            }
        }

        private static GUIStyle conditionYellow;
        public static GUIStyle ConditionYellow
        {
            get
            {
                if(conditionYellow == null)
                {
                    conditionYellow = new GUIStyle();
                    conditionYellow.normal.textColor = Color.black;
                    conditionYellow.alignment = TextAnchor.UpperCenter;
                    conditionYellow.normal.background = Resources.Load<Texture2D>("EditorStyles/Yellow"); ;
                }
                return conditionYellow;
            }
        }

        private static GUIStyle falseRed;
        public static GUIStyle FalseRed
        {
            get
            {
                if(falseRed == null)
                {
                    falseRed = new GUIStyle();
                    falseRed.normal.textColor = Color.black;
                    falseRed.alignment = TextAnchor.UpperCenter;
                    falseRed.normal.background = Resources.Load<Texture2D>("EditorStyles/Red");
                    falseRed.border = new RectOffset(4, 4, 12, 12);
                }
                return falseRed;
            }
        }

        private static GUIStyle triggerBlue;
        public static GUIStyle TriggerBlue
        {
            get
            {
                if (triggerBlue == null)
                {
                    triggerBlue = new GUIStyle();
                    triggerBlue.normal.textColor = Color.black;
                    triggerBlue.alignment = TextAnchor.UpperCenter;
                    triggerBlue.normal.background = Resources.Load<Texture2D>("EditorStyles/Blue");
                }
                return triggerBlue;
            }
        }

        private static GUIStyle logicPurple;
        public static GUIStyle LogicPurple
        {
            get
            {
                if (logicPurple == null)
                {
                    logicPurple = new GUIStyle();
                    logicPurple.normal.textColor = Color.white;
                    logicPurple.alignment = TextAnchor.UpperCenter;
                    logicPurple.normal.background = Resources.Load<Texture2D>("EditorStyles/Purple");
                }
                return logicPurple;
            }
        }

        private static GUIStyle inPoint;
        public static GUIStyle InPoint
        {
            get
            {
                //Setup the static style for connectors.
                if (inPoint == null)
                {
                    inPoint = new GUIStyle();
                    inPoint.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn left.png") as Texture2D;
                    inPoint.active.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn left on.png") as Texture2D;
                    inPoint.border = new RectOffset(4, 4, 12, 12);
                }
                return inPoint;
            }
        }


        private static GUIStyle outPoint;
        public static GUIStyle OutPoint
        {
            get
            {
                if(outPoint == null)
                {
                    outPoint = new GUIStyle();
                    outPoint.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn right.png") as Texture2D;
                    outPoint.active.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn right on.png") as Texture2D;
                    outPoint.border = new RectOffset(4, 4, 12, 12);
                }
                return outPoint;
            }
        }
    }
}
#endif