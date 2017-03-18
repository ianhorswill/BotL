#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="KnowledgeBase.cs" company="Ian Horswill">
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
using System;
using UnityEngine;
using System.IO;

namespace BotL.Unity
{
    [AddComponentMenu("BotL/Knowledge base")]
    public class KnowledgeBase : MonoBehaviour
    {
        [Serializable]
        public class NamespaceImport
        {
            public string Namespace;
            public string DLLName = "Assembly-CSharp";
        }

        public NamespaceImport[] NamespaceImports;
        public string[] SourceFiles;

        internal void Start()
        {
            foreach (var n in NamespaceImports)
                TypeUtils.AddTypeSearchPath(n.Namespace, n.DLLName);
            foreach (var f in SourceFiles)
            {
                LoadSource(f);
            }
        }

        private static void LoadDirectory(string dpath)
        {
            foreach (var f in Directory.GetFiles(dpath))
                LoadSource(f);
        }

        private static void LoadSource(string f)
        {
            var ext = Path.GetExtension(f) ?? "";
            switch (ext)
            {
                case "bot":
                    KB.Load(f);
                    break;

                case "csv":
                    KB.LoadTable(f);
                    break;

                case "":
                    f = UnityUtilities.CanonicalizePath(f);
                    if (Directory.Exists(f))
                        LoadDirectory(f);
                    else
                        KB.Load(f);
                    break;

                default:
                    Debug.Log("Unknown source file: "+f);
                    break;
            }
        }
    }
}
