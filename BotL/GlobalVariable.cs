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
    public class GlobalVariable
    {
        static readonly Dictionary<Symbol, GlobalVariable> GlobalVariables = new Dictionary<Symbol, GlobalVariable>();

        public static readonly GlobalVariable This = DefineGlobal("this", null);
        public static readonly GlobalVariable GameObject = DefineGlobal("gameobject", null);
        public static readonly GlobalVariable Time = DefineGlobal("time", null);

        public static GlobalVariable DefineGlobal(string name, object initialValue)
        {
            var n = Symbol.Intern(name);
            if (!GlobalVariables.ContainsKey(n))
                GlobalVariables[n] = new GlobalVariable(n, initialValue);
            return GlobalVariables[n];
        }

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
        
        // ReSharper disable once MemberCanBePrivate.Global
        public readonly Symbol Name;
        public TaggedValue Value;

        public override string ToString()
        {
            return $"${Name.Name}";
        }
    }
}
