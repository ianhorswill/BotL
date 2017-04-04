#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Console.cs" company="Ian Horswill">
// Copyright (C) 2017 Ian Horswill
//  
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in the
// Software without restriction, including without limitation the rights to use, copy,
// modify, merge, publish, distribute, sublicense, and/or sell copies of the Software,
// and to permit persons to whom the Software is furnished to do so, subject to the
// following conditions:
//  
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
// PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
#endregion
using System.Collections.Generic;
using System.IO;
using System.Text;
using BotL;
using UnityEngine;
// ReSharper disable MemberCanBePrivate.Global

//
// Console window driver by Lee Fan.
// Modifications by Ian Horswill
//

namespace Northwestern.UnityUtils
{
    public class Console : MonoBehaviour
    {
        public int MaxTextLength = 4096;
        public Rect WindowRect = new Rect(0, 0, 640, 480); //Defines console size and dimensions
        // ReSharper disable once MemberCanBeProtected.Global
        public string WindowTitle = "Console";
        public string Header = ""; //First thing shown when console starts

        public KeyCode ActivationKey = KeyCode.F2;	//Key used to show/hide console
        public bool ShowConsole;				//Whether or not console is visible
        public bool PopupOnWrite;


        //Public variables for writing to console stdout and stdin
        public string In;

        protected ConsoleWriter Out;

        /// <summary>
        /// CharacterNameStyle in which to display text.
        /// </summary>
        // ReSharper disable once UnassignedField.Global
        public GUIStyle Style;

        // ReSharper disable once InconsistentNaming
        protected static int IDCount = typeof(Console).GetHashCode();

        // ReSharper disable once InconsistentNaming
        private int ID; //unique generated ID

        // ReSharper disable once InconsistentNaming
        private string consoleID; //generated from ID

        private string consoleBuffer; //Tied to GUI.Label

        private Vector2 scrollPosition;

        // Set when written to output; forces scroll to bottom left.
        private bool forceScrollToEnd;

        private bool firstFocus; //Controls console input focus

        /// <summary>
        /// List of all the things the commands the user has typed
        /// </summary>
        private List<string> history;
        /// <summary>
        /// Position in the history list when recalling previous commands
        /// </summary>
        private int historyPosition;

        /// <summary>
        /// Initializes console properties and sets up environment.
        /// </summary>
        // ReSharper disable once UnusedMemberHierarchy.Global
        internal virtual void Start()
        {
            Initialize();
            In = string.Empty;
            Out = new ConsoleWriter(MaxTextLength);
            Out.WriteLine(Header);
            consoleBuffer = Out.GetTextUpdate();
            scrollPosition = Vector2.zero;
            ID = IDCount++;
            consoleID = "window" + ID;
            history = new List<string>();
        }

        /// <summary>
        /// Creates the Console Window.
        /// </summary>
        /// <param name='windowID'>
        /// unused parameter.
        /// </param>
        // ReSharper disable once InconsistentNaming
        private void DoConsoleWindow(int windowID)
        {
            //Console Window
            GUI.DragWindow(new Rect(0, 0, WindowRect.width, 20));
            //Scroll Area
            scrollPosition = GUILayout.BeginScrollView(
                scrollPosition,
                GUILayout.MaxHeight(WindowRect.height - 48),
                GUILayout.ExpandHeight(false),
                GUILayout.Width(WindowRect.width - 15));
            //Console Buffer
            GUILayout.Label(consoleBuffer, Style, GUILayout.ExpandHeight(true));
            GUILayout.EndScrollView();
            if (forceScrollToEnd)
            {
                scrollPosition = new Vector2(0, Mathf.Infinity);
                forceScrollToEnd = false;
            }
            //Input Box
            GUI.SetNextControlName(consoleID);
            In = GUI.TextField(new Rect(4, WindowRect.height - 24, WindowRect.width - 8, 20), In, Style);
            if (firstFocus)
            {
                GUI.FocusControl(consoleID);
                firstFocus = false;
            }
        }

        // ReSharper disable once UnusedMember.Global
        internal void OnGUI()
        {
            if (PopupOnWrite)
                ShowConsole |= Out.IsUpdated();

            if (ShowConsole)
            {
                WindowRect = GUI.Window(ID, WindowRect, DoConsoleWindow, WindowTitle);
            }
            if (Event.current.isKey && Event.current.type == EventType.KeyUp)
            {
                var weAreFocus = GUI.GetNameOfFocusedControl() == consoleID;
                if (Event.current.keyCode == ActivationKey)
                {
                    ShowConsole = !ShowConsole;
                    firstFocus = true;
                }
                switch (Event.current.keyCode)
                {
                    case KeyCode.Return:
                        if (weAreFocus && In != string.Empty)
                        {
                            scrollPosition = GUI.skin.label.CalcSize(new GUIContent(consoleBuffer));
                            string command = In;
                            In = string.Empty;
                            if (!OmitCommandFromHistory(command))
                            {
                                history.Add(command);
                                historyPosition = history.Count;
                            }
                            Run(command);
                        }
                        break;

                    case KeyCode.UpArrow:
                        if (historyPosition > 0)
                        {
                            In = history[--historyPosition];
                        }
                        break;

                    case KeyCode.DownArrow:
                        if (historyPosition < history.Count-1)
                        {
                            In = history[++historyPosition];
                        }
                        break;
                }
                if (weAreFocus)
                    Event.current.Use();
            }
            if (Out != null && Out.IsUpdated())
            {
                consoleBuffer = Out.GetTextUpdate();
                forceScrollToEnd = true;
            }
        }

        /// <summary>
        /// A TextWriter for output buffer
        /// </summary>
        protected class ConsoleWriter : TextWriter
        {
            private bool bufferUpdated;
            private int maxTextLength;

            //tracks when changes are made to StringBuilder to prevent generating new strings every click

            private readonly StringBuilder oBuffer;

            public ConsoleWriter(int maxTextLength)
            {
                this.maxTextLength = maxTextLength;
                oBuffer = new StringBuilder();
            }

            public override Encoding Encoding => Encoding.Default;

            public override void Write(string value)
            {
                oBuffer.Append(value);
                oBuffer.Append(System.Environment.StackTrace);
                bufferUpdated = true;
            }

            public override void WriteLine(string value)
            {
                oBuffer.AppendLine(value);
                oBuffer.Append(System.Environment.StackTrace);
                bufferUpdated = true;
            }

            public override void WriteLine()
            {
                WriteLine("");
            }

            public string GetTextUpdate()
            {
                bufferUpdated = false;
                var len = oBuffer.Length;
                if (len > maxTextLength)
                    oBuffer.Remove(0, len - maxTextLength);
                return oBuffer.ToString();
            }

            public bool IsUpdated()
            {
                return bufferUpdated;
            }
        }

        /// <summary>
        /// Run when a newline is entered in the input box.
        /// </summary>
        /// <param name='command'>
        /// The entered text prior to the newline.
        /// </param>
        protected virtual void Run(string command)
        {
            Out.WriteLine(">> " + command);
            //override for functionality
        }

        /// <summary>
        /// Allows for initialization of <code>Width</code>, <code>Height</code>, <code>Header</code>, <code>ActivationKey</code>, and <code>showConsole</code>
        /// </summary>
        protected void Initialize()
        {
            //override to set console properties
        }

        // ReSharper disable once UnusedParameter.Global
        protected bool OmitCommandFromHistory(string command)
        {
            return false;
        }
    }
}
