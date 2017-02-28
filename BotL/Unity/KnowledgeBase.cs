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
