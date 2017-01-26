﻿#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExtensionMethods.cs" company="Ian Horswill">
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
using UnityEngine;

namespace BotL.Unity
{
    // ReSharper disable once UnusedMember.Global
    public static class ExtensionMethods
    {
        /// <summary>
        /// Run the specified predicate with no arguments.  Returns true if it succeeds.
        /// </summary>
        /// <param name="comp">Component calling predicate; used to set $this and $gameobject.</param>
        /// <param name="predicateName">Name of BotL predicate to run.</param>
        /// <returns>Success or failure</returns>
        // ReSharper disable once UnusedMember.Global
        public static bool IsTrue(this Component comp, string predicateName)
        {
            UnityUtilities.SetUnityGlobals(comp.gameObject, comp);
            return Engine.Run(predicateName);
        }
        
        /// <summary>
        /// Run the specified predicate with no arguments.  Returns true if it succeeds.
        /// </summary>
        /// <param name="gameObject">GameObject calling predicate; used to set $gameobject.</param>
        /// <param name="predicateName">Name of BotL predicate to run.</param>
        /// <returns>Success or failure</returns>
        // ReSharper disable once UnusedMember.Global
        public static bool IsTrue(this GameObject gameObject, string predicateName)
        {
            UnityUtilities.SetUnityGlobals(gameObject, null);
            return Engine.Run(predicateName);
        }
    }
}
