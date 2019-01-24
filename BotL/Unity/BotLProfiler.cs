using System.Collections.Generic;
using UnityEngine;

namespace BotL.Unity
{
    [AddComponentMenu("BotL/Profiler")]
    public class BotLProfiler : MonoBehaviour
    {
        #region Editor-configurable properties
        // ReSharper disable once MemberCanBePrivate.Global
        public Rect WindowRect = new Rect(0, 0, 640, 480);
        // ReSharper disable once MemberCanBePrivate.Global
        public bool ShowInspector = true;
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        public GUIStyle Style = new GUIStyle();
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        public KeyCode ActivationKey = KeyCode.F3;	//Key used to show/hide inspector
        // Amount by which to indent children relative to parents
        public int Indentation = 10;
        private const string controlName = "BotLProfiler";
        #endregion

#if BotLProfiler
        #region Private fields and properties
        // ReSharper disable once InconsistentNaming
        private int ID;
        private Vector2 scrollPosition;
        // ReSharper disable once InconsistentNaming
        private static int IDCount = typeof(ELInspector).GetHashCode();

        /// <summary>
        /// Total height of the currently displayed profiling data
        /// </summary>
        private float viewHeight;
        #endregion


        // ReSharper disable once UnusedMember.Global
        internal void Start()
        {
            ID = IDCount++;
            viewHeight = WindowRect.height;
            Profiler.EnableProfiling();
        }

        private bool mouseClicked;
        private float mouseClickY;

        // ReSharper disable once UnusedMember.Global
        internal void OnGUI()
        {
            if (ShowInspector)
            {
                WindowRect = GUI.Window(ID, WindowRect, DrawWindow, "Profiler data");
            }

            switch (Event.current.type)
            {
                case EventType.MouseDown:
                    if(ShowInspector && WindowRect.Contains(Event.current.mousePosition))
                    {
                        GUI.FocusControl(controlName);
                        mouseClicked = true;
                        mouseClickY = Event.current.mousePosition.y - WindowRect.y;
                        Event.current.Use();
                    }
                    break;

                case EventType.KeyUp:
                    if (Event.current.keyCode == ActivationKey)
                    {
                        ShowInspector = !ShowInspector;
                    }
                    else if (GUIUtility.keyboardControl == controlID)
                    {
                        switch (Event.current.keyCode)
                        {
                            case KeyCode.PageDown:
                                scrollPosition.y += WindowRect.height * 0.5f;
                                break;

                            case KeyCode.PageUp:
                                scrollPosition.y -= Mathf.Max(0, WindowRect.height * 0.5f);
                                break;
                        }
                        Event.current.Use();
                    }
                    break;
            }
        }

        // ReSharper disable once InconsistentNaming
        private void DrawWindow(int windowID)
        {
            //Console Window
            GUI.DragWindow(new Rect(0, 0, WindowRect.width, 20));
            //Scroll Area
            scrollPosition =
                GUI.BeginScrollView(
                    new Rect(0, 0, WindowRect.width, WindowRect.height),
                    scrollPosition,
                    new Rect(0, 0, WindowRect.width, viewHeight), false, true);
            mouseClickY += scrollPosition.y;
            viewHeight = Mathf.Max(
                viewHeight,
                RenderChildren(Profiler.CallTreeRoot, 10, Style.lineHeight));
            GUI.EndScrollView();
            mouseClicked = false;
            GUI.SetNextControlName(controlName);
            controlID = GUIUtility.GetControlID(FocusType.Keyboard);
        }

        private float RenderAt(Profiler.ProfileNode node, float x, float y)
        {
            var text = node.ToString();
            
            var lineHeight = Style.lineHeight;
            if (mouseClickY >= y && mouseClickY < y + lineHeight)
            {
                text = "<b>" + text + "</b>";
                if (mouseClicked)
                    ToggleNode(node);
            }
            GUI.Label(new Rect(x, y, 10000, lineHeight), new GUIContent(text), Style);
            y += lineHeight;

            if (NodeExpanded(node))
                y = RenderChildren(node, x + Indentation, y);

            return y;
        }

        private float RenderChildren(Profiler.ProfileNode node, float x, float y)
        {
            node.SortChildren();
            foreach (var c in node.Children)
                y = RenderAt(c, x, y);
            return y;
        }

        #region Node expansion control
        private static readonly HashSet<Profiler.ProfileNode> ExpandedNodes = new HashSet<Profiler.ProfileNode>();
        private int controlID;

        bool NodeExpanded(Profiler.ProfileNode node)
        {
            return ExpandedNodes.Contains(node);
        }

        void ToggleNode(Profiler.ProfileNode node)
        {
            if (ExpandedNodes.Contains(node))
                ExpandedNodes.Remove(node);
            else
                ExpandedNodes.Add(node);
        }
        #endregion
#endif
    }
}
