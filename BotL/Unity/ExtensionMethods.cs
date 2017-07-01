#region Copyright
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

using JetBrains.Annotations;
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
        /// Run the specified predicate with the specified arguments.  Returns true if it succeeds.
        /// WARNING: this uses a params argument, so it allocates memory.
        /// </summary>
        /// <param name="comp">Component calling predicate; used to set $this and $gameobject.</param>
        /// <param name="predicateName">Name of BotL predicate to run.</param>
        /// <returns>Success or failure</returns>
        /// <param name="arguments">Arguments to pass to predicate.</param>
        [UsedImplicitly]
        public static bool IsTrue(this Component comp, string predicateName, params object[] arguments)
        {
            UnityUtilities.SetUnityGlobals(comp.gameObject, comp);
            return Engine.Apply(predicateName, arguments);
        }

        /// <summary>
        /// Run the specified predicate with the specified arguments, followed by an extra, unbound,
        /// output argument.  Returns the value of the output argument.  Thus, FunctionValue("=", 1)
        /// would run =(1,R), and return the value of R (which would be 1).
        /// WARNING: this uses a params argument, so it allocates memory.
        /// </summary>
        /// <param name="comp">Component calling predicate; used to set $this and $gameobject.</param>
        /// <param name="predicateName">Name of BotL predicate to run.</param>
        /// <returns>Success or failure</returns>
        /// <param name="arguments">Arguments to pass to predicate.</param>
        [UsedImplicitly]
        public static T FunctionValue<T>(this Component comp, string predicateName, params object[] arguments)
        {
            UnityUtilities.SetUnityGlobals(comp.gameObject, comp);
            return Engine.ApplyFunction<T>(predicateName, arguments);
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

        /// <summary>
        /// Run the specified predicate with the specified arguments.  Returns true if it succeeds.
        /// WARNING: uses a params argument, so it allocates memory
        /// </summary>
        /// <param name="gameObject">GameObject calling predicate; used to set $gameobject.</param>
        /// <param name="predicateName">Name of BotL predicate to run.</param>
        /// <returns>Success or failure</returns>
        /// <param name="arguments">Arguments to pass to predicate.</param>
        [UsedImplicitly]
        public static bool IsTrue(this GameObject gameObject, string predicateName, params object[] arguments)
        {
            UnityUtilities.SetUnityGlobals(gameObject, null);
            return Engine.Apply(predicateName, arguments);
        }

        /// <summary>
        /// Run the specified predicate with the specified arguments, followed by an extra, unbound,
        /// output argument.  Returns the value of the output argument.  Thus, FunctionValue("=", 1)
        /// would run =(1,R), and return the value of R (which would be 1).
        /// WARNING: this uses a params argument, so it allocates memory.
        /// </summary>
        /// <param name="gameObject">GameObject calling predicate; used to set $gameobject; $this is set to null.</param>
        /// <param name="predicateName">Name of BotL predicate to run.</param>
        /// <returns>Success or failure</returns>
        /// <param name="arguments">Arguments to pass to predicate.</param>
        [UsedImplicitly]
        public static T FunctionValue<T>(this GameObject gameObject, string predicateName, params object[] arguments)
        {
            UnityUtilities.SetUnityGlobals(gameObject, null);
            return Engine.ApplyFunction<T>(predicateName, arguments);
        }

        /// <summary>
        /// Run the specified predicate with no arguments.  Throw exception if it fails.
        /// Intended for calling imperatives.
        /// </summary>
        /// <param name="comp">Component calling predicate; used to set $this and $gameobject.</param>
        /// <param name="predicateName">Name of BotL predicate to run.</param>
        // ReSharper disable once UnusedMember.Global
        public static void RunBotL(this Component comp, string predicateName)
        {
            if (!comp.IsTrue(predicateName))
                throw new CallFailedException(Symbol.Intern(predicateName));
        }

        /// <summary>
        /// Run the specified predicate with the specified arguments.  Throw exception if it fails.
        /// Intended for calling imperatives.
        /// WARNING: uses a params argument, so allocates memory
        /// </summary>
        /// <param name="comp">Component calling predicate; used to set $this and $gameobject.</param>
        /// <param name="predicateName">Name of BotL predicate to run.</param>
        /// <param name="arguments">Arguments to pass to predicate.</param>
        [UsedImplicitly]
        public static void RunBotL(this Component comp, string predicateName, params object[] arguments)
        {
            if (!comp.IsTrue(predicateName, arguments))
                throw new CallFailedException(Symbol.Intern(predicateName));
        }

        /// <summary>
        /// Run the specified predicate with no arguments.  Throw exception if it fails.
        /// Intended for calling imperatives.
        /// </summary>
        /// <param name="gameObject">GameObject calling predicate; used to set $gameobject.</param>
        /// <param name="predicateName">Name of BotL predicate to run.</param>
        // ReSharper disable once UnusedMember.Global
        public static void RunBotL(this GameObject gameObject, string predicateName)
        {
            if (!gameObject.IsTrue(predicateName))
                throw new CallFailedException(Symbol.Intern(predicateName));
        }

        /// <summary>
        /// Run the specified predicate with the specified arguments.  Throw exception if it fails.
        /// Intended for calling imperatives.
        /// WARNING: uses a params argument, so allocates memory.
        /// </summary>
        /// <param name="gameObject">GameObject calling predicate; used to set $gameobject.</param>
        /// <param name="predicateName">Name of BotL predicate to run.</param>
        /// <param name="arguments">Arguments to pass to predicate.</param>
        [UsedImplicitly]
        public static void RunBotL(this GameObject gameObject, string predicateName, params object[] arguments)
        {
            if (!gameObject.IsTrue(predicateName, arguments))
                throw new CallFailedException(Symbol.Intern(predicateName));
        }
    }
}
