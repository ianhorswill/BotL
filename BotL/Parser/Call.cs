﻿#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Call.cs" company="Ian Horswill">
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
using System.Collections;
using BotL.Parser;

namespace BotL
{
    public class Call : IList
    {
        public Call(Symbol f, params object[] args)
        {
            Functor = f;
            Arguments = args;
        }

        public Call(string s, params object[] args) : this(Symbol.Intern(s), args)
        {
        }

        public readonly Symbol Functor;
        public readonly object[] Arguments;

        public object this[int i]
        {
            get
            {
                if (i == 0)
                    return Functor;
                return Arguments[i-1];
            }
            set { throw new NotImplementedException(); }
        }

        public int Arity => Arguments.Length;

        public override string ToString()
        {
            return ExpressionParser.WriteExpressionToString(this);
        }

        public static bool IsFunctor(object o, Symbol functor, int arity)
        {
            Call c = o as Call;
            return c != null && c.Functor == functor && c.Arity == arity;
        }

        internal bool IsFunctor(Symbol functor, int arity)
        {
            return Functor == functor && Arity == arity;
        }

        internal static object AddArgument(object term, object arg)
        {
            var c = term as Call;
            if (c == null)
                return new Call((Symbol) term, arg);
            return c.AddArgument(arg);
        }

        internal Call AddArgument(object t)
        {
            var extendedArgs = new object[Arity + 1];
            Array.Copy(Arguments, extendedArgs, Arguments.Length);
            extendedArgs[extendedArgs.Length - 1] = t;
            return new Call(Functor, extendedArgs);
        }

        public IEnumerator GetEnumerator()
        {
            yield return Functor;
            foreach (var arg in Arguments)
                yield return arg;
        }

        public void CopyTo(Array array, int index)
        {
            for (int i=0; i < Count; i++)
                array.SetValue(this[i], index+i); 
        }

        public bool IsReadOnly => true;
        public bool IsFixedSize => true;

        public int Count => Arguments.Length + 1;
        public object SyncRoot => this;
        public bool IsSynchronized => false;
        public int Add(object value)
        {
            throw new NotImplementedException();
        }

        public bool Contains(object value)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public int IndexOf(object value)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, object value)
        {
            throw new NotImplementedException();
        }

        public void Remove(object value)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }
    }
}
