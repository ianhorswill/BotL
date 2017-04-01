#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NamedEntities.cs" company="Ian Horswill">
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
using BotL.Unity;

namespace BotL
{
    static class NamedEntities
    {
        public static object Resolve(object name)
        {
            string n = name as string;
            if (n == null)
            {
                var s = name as Symbol;
                if (s == null)
                    throw new ArgumentException(nameof(name));
                n = s.Name;
            }
            var v = GlobalVariable.Find(Symbol.Intern(n));
            if (v != null)
                return v;

            var type = TypeUtils.FindType(n);
            if (type != null)
                return type;

            var unityObject = UnityUtilities.TryGetUnityObject(n);
            if (unityObject != null)
                return unityObject;

            foreach (var r in Resolvers)
                if (r(n, out object result))
                    return result;

            throw new ArgumentException("Unknown type or game object: "+n);
        }

        /// <summary>
        ///  A user-defined method for resolving $ and # names to their referrents
        /// </summary>
        /// <param name="name">Name of object to look up</param>
        /// <param name="value">Object found, if any.</param>
        /// <returns>True if this resolver found an object with that name.</returns>
        public delegate bool Resolver(object name, out object value);

        private static readonly List<Resolver> Resolvers = new List<Resolver>();

        /// <summary>
        /// Add a user-defined name resolver for $ and # expressions
        /// </summary>
        /// <param name="resolver">Delegate that maps names to the things the refer to.</param>
        public static void RegisterNameResolver(Resolver resolver)
        {
            Resolvers.Add(resolver);
        }
    }
}
