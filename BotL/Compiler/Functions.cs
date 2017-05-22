#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Functions.cs" company="Ian Horswill">
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
using System.Collections.Generic;

namespace BotL
{
    /// <summary>
    /// Table of predicates that are callable as functions
    /// </summary>
    public static class Functions
    {
        private static readonly HashSet<PredicateIndicator> HoistableFunctions = new HashSet<PredicateIndicator>();

        public static void DeclareFunction(string name, int arity)
        {
            HoistableFunctions.Add(new PredicateIndicator(Symbol.Intern(name), arity));
        }

        internal static void DeclareFunction(PredicateIndicator p)
        {
            HoistableFunctions.Add(new PredicateIndicator(p.Functor, p.Arity - 1));
        }

        public static bool IsFunctionRelation(object exp)
        {
            if (exp is Symbol || exp is Call)
                return HoistableFunctions.Contains(new PredicateIndicator(exp));
            return false;
        }

        public static void DeclareFunction(string name, Func<int, int> f)
        {
            var n = Symbol.Intern(name);
            new UserFunction(n, 1, stack =>
            {
                if (Engine.DataStack[stack-1].Type != TaggedValueType.Integer)
                    throw new ArgumentTypeException(n.Name, 1, "Should be an integer", Engine.DataStack[stack-1].Value);
                Engine.DataStack[stack-1].Set(f(Engine.DataStack[stack-1].integer));
                return stack;
            });
        }

        public static void DeclareFunction(string name, Func<float, float> f)
        {
            var n = Symbol.Intern(name);
            new UserFunction(n, 1, stack =>
            {
                var arg1Type = Engine.DataStack[stack-1].Type;
                if (arg1Type != TaggedValueType.Integer && arg1Type != TaggedValueType.Float)
                    throw new ArgumentTypeException(n.Name, 1, "Should be a number", Engine.DataStack[stack-1].Value);
                Engine.DataStack[stack-1].Set(f(Engine.DataStack[stack-1].AsFloat));
                return stack;
            });
        }

        public static void DeclareFunction<T>(string name, Func<T, object> f) where T:class
        {
            var n = Symbol.Intern(name);
            new UserFunction(n, 1, stack =>
            {
                if (Engine.DataStack[stack-1].Type != TaggedValueType.Reference || !(Engine.DataStack[stack-1].reference is T arg))
                    throw new ArgumentTypeException(n.Name, 1, "Should be a "+typeof(T).Name, Engine.DataStack[stack-1].Value);
                Engine.DataStack[stack-1].SetReference(f(arg));
                return stack;
            });
        }
    }
}
