#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GlobalVariable.cs" company="Ian Horswill">
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

namespace BotL
{
    /// <summary>
    /// A "global variable" within BotL.  These can be used as normal global variables, 
    /// or as a communication mechanism with C# code.
    /// </summary>
    public class GlobalVariable
    {
        /// <summary>
        /// Table of global variables that have been created.
        /// </summary>
        static readonly Dictionary<Symbol, GlobalVariable> GlobalVariables = new Dictionary<Symbol, GlobalVariable>();

        /// <summary>
        /// The $this global
        /// </summary>
        public static readonly GlobalVariable This = DefineGlobal("this", null);
        /// <summary>
        /// The $gameobject global
        /// </summary>
        public static readonly GlobalVariable GameObject = DefineGlobal("gameobject", null);
        /// <summary>
        /// The $time global
        /// </summary>
        public static readonly GlobalVariable Time = DefineGlobal("time", null);

        /// <summary>
        /// Create a new global variable that can be accessed by BotL code and return it so it can also
        /// be accessed by C# code.
        /// </summary>
        /// <param name="name">Name to use for this global from within BotL code (do not include the $).</param>
        /// <param name="initialValue">Initial value for the global.</param>
        /// <returns></returns>
        public static GlobalVariable DefineGlobal(string name, object initialValue)
        {
            var n = Symbol.Intern(name);
            if (!GlobalVariables.ContainsKey(n))
                GlobalVariables[n] = new GlobalVariable(n, initialValue);
            return GlobalVariables[n];
        }

        /// <summary>
        /// Return the GlobalVariable object with the specified name.
        /// </summary>
        /// <param name="name">Name without $</param>
        /// <returns>The GlobalVariable object representing it.</returns>
        public static GlobalVariable Find(Symbol name)
        {
            GlobalVariable v;
            if (!GlobalVariables.TryGetValue(name, out v))
                return null;
            return v;
        }

        private GlobalVariable(Symbol name, object value)
        {
            Name = name;
            Value.SetGeneral(value);
        }
        
        /// <summary>
        /// The name of the global variable (without the $)
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public readonly Symbol Name;
        /// <summary>
        /// The current value fo the global.
        /// </summary>
        public TaggedValue Value;

        public override string ToString()
        {
            return $"${Name.Name}";
        }
    }
}
