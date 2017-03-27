#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UnityUtilities.cs" company="Ian Horswill">
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
using System.IO;
using UnityEngine;
using static BotL.Engine;

namespace BotL.Unity
{
    /// <summary>
    /// Linkage to Unity stuff
    /// </summary>
    internal static class UnityUtilities
    {
        /// <summary>
        /// Implements GetComponent operation for functional expressions.
        /// </summary>
        /// <param name="stack">Pointer to the top of the functional expression eval stack</param>
        /// <returns>Address in stack where result is left.</returns>
        internal static ushort LookupUnityComponent(ushort stack)
        {
            // We have to put this is in a separate method or the unit tests throw a security exception
            // when the jitter tries to compile Eval.  Putting it here means the references to UnityEngine
            // don't get jitted during the unit tests.
            Type t = (Type)DataStack[--stack].reference;
            --stack;
            if (DataStack[stack].Type != TaggedValueType.Reference
                || !(DataStack[stack].reference is GameObject))
                throw new ArgumentException("Argument to : is not a GameObject: " + DataStack[stack].Value);
            GameObject go = (GameObject)DataStack[stack].reference;
            DataStack[stack++].reference = go.GetComponent(t);
            return stack;
        }
        
        /// <summary>
        /// Look up a unity gameobject by name.
        /// </summary>
        /// <param name="name">Name of the game object</param>
        /// <returns>The game object or null</returns>
        internal static object TryGetUnityObject(string name)
        {
            // We need this in its own method because if we inline it into FunctionalExpresions.Resolve
            // then the unit tests get a security exception when Resolve gets JITed because
            // the unit tests don't actually run inside Unity.
            return GameObject.Find(name);
        }

        /// <summary>
        /// Convert relative paths to be relative to Assets directory
        /// </summary>
        /// <param name="path">Path</param>
        /// <returns>Full path</returns>
        internal static string CanonicalizePath(string path)
        {
            if (Path.IsPathRooted(path))
                return path;
            return PathWithinAssets(path);
        }

        // This has to be in a separate method from CanonicalizePath in order for the latter
        // to be callable in the standalone repl and in the unit tests.
        private static string PathWithinAssets(string path)
        {
            return Path.Combine(Application.dataPath, path);
        }

        /// <summary>
        /// Set $this, $gameobject, and $time in preparation for invoking the interpreter
        /// </summary>
        /// <param name="gameObject">Value for $gameobject</param>
        /// <param name="comp">Value for $this</param>
        public static void SetUnityGlobals(GameObject gameObject, Component comp)
        {
            GlobalVariable.Time.Value.Set(Time.time);
            GlobalVariable.This.Value.SetReference(comp);
            GlobalVariable.GameObject.Value.SetReference(gameObject);
        }

        public static float Distance(object arg1, object arg2)
        {
            if (!(arg1 is Vector3 v1))
            {
                if ((arg1 is GameObject o1))
                    v1 = o1.transform.position;
                else
                    throw new ArgumentTypeException("distance", 1, "Argument should be a GameObject", arg1);
            }

            if (!(arg2 is Vector3 v2))
            {
                if ((arg2 is GameObject o2))
                    v2 = o2.transform.position;
                else
                    throw new ArgumentTypeException("distance", 2, "Argument should be a GameObject", arg2);
            }

            return Vector3.Distance(v1, v2);
        }

        public static float DistanceSq(object arg1, object arg2)
        {
            if (!(arg1 is GameObject o1))
                throw new ArgumentTypeException("distance", 1, "Argument should be a GameObject", arg1);
            if (!(arg2 is GameObject o2))
                throw new ArgumentTypeException("distance", 2, "Argument should be a GameObject", arg1);

            return Vector3.SqrMagnitude(o1.transform.position-o2.transform.position);
        }
    }
}
