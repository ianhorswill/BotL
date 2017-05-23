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
using System.Runtime.InteropServices.ComTypes;

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
                Engine.DataStack[stack-1].Set(f(IntArg(name, stack, 1)));
                return stack;
            });
        }

        public static void DeclareFunction(string name, Func<int, int, int> f)
        {
            var n = Symbol.Intern(name);
            new UserFunction(n, 2, stack =>
            {
                Engine.DataStack[stack - 2].Set(f(IntArg(name, stack, 1), IntArg(name, stack, 2)));
                return (ushort)(stack-1);
            });
        }

        public static void DeclareFunction(string name, Func<float, float> f)
        {
            var n = Symbol.Intern(name);
            new UserFunction(n, 1, stack =>
            {
                Engine.DataStack[stack - 1].Set(f(FloatArg(name, stack, 1)));
                return stack;
            });
        }

        public static void DeclareFunction(string name, Func<float, float, float> f)
        {
            var n = Symbol.Intern(name);
            new UserFunction(n, 2, stack =>
            {
                Engine.DataStack[stack - 2].Set(f(FloatArg(name, stack, 1), FloatArg(name, stack, 2)));
                return (ushort)(stack - 1);
            });
        }

        public static void DeclareFunction<T>(string name, Func<T, object> f) where T : class
        {
            var n = Symbol.Intern(name);
            new UserFunction(n, 1, stack =>
            {
                Engine.DataStack[stack - 1].SetReference(f(ReferenceArg<T>(name, stack, 1)));
                return stack;
            });
        }

        public static void DeclareFunction<T1,T2>(string name, Func<T1, T2, object> f) 
            where T1 : class
            where T2 : class
        {
            var n = Symbol.Intern(name);
            new UserFunction(n, 2, stack =>
            {
                Engine.DataStack[stack - 2].SetReference(f(ReferenceArg<T1>(name, stack, 1), ReferenceArg<T2>(name, stack, 2)));
                return (ushort)(stack - 1);
            });
        }

        private static int IntArg(string functionName, ushort stack, int argumentIndex)
        {
            if (Engine.DataStack[stack - argumentIndex].Type != TaggedValueType.Integer)
                throw new ArgumentTypeException(functionName, argumentIndex, "Should be an integer",
                    Engine.DataStack[stack - argumentIndex].Value);
            var arg = Engine.DataStack[stack - argumentIndex].integer;
            return arg;
        }

        private static float FloatArg(string name, ushort stack, int argumentIndex)
        {
            var arg1Type = Engine.DataStack[stack - argumentIndex].Type;
            if (arg1Type != TaggedValueType.Integer && arg1Type != TaggedValueType.Float)
                throw new ArgumentTypeException(name, argumentIndex, "Should be a number",
                    Engine.DataStack[stack - argumentIndex].Value);
            var asFloat = Engine.DataStack[stack - argumentIndex].AsFloat;
            return asFloat;
        }

        private static T ReferenceArg<T>(string funcName, ushort stack, int argumentIndex) where T : class
        {
            if (Engine.DataStack[stack - argumentIndex].Type != TaggedValueType.Reference ||
                !(Engine.DataStack[stack - argumentIndex].reference is T arg))
                throw new ArgumentTypeException(funcName, argumentIndex, "Should be a " + typeof(T).Name,
                    Engine.DataStack[stack - argumentIndex].Value);
            return arg;
        }
    }
}
