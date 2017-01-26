#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Predicate.cs" company="Ian Horswill">
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
using System.Diagnostics;

namespace BotL
{
    [DebuggerDisplay("{Name}/{Arity}")]
    public sealed class Predicate
    {
        internal Predicate(Symbol name, int arity, Compiler.Compiler.PredicateImplementation primopImplementation = null, Table t = null, byte tempvars = 0)
        {
            Name = name;
            Arity = arity;
            PrimopImplementation = primopImplementation;
            IsLocked = primopImplementation != null || t != null;
            Table = t;
            Tempvars = tempvars;
        }

        public readonly Symbol Name;
        public readonly int Arity;
        internal CompiledClause FirstClause;
        internal List<CompiledClause> ExtraClauses;
        private List<object> objectConstants;
        private List<int> intConstants;
        private List<float> floatConstants;
        public bool IsLocked;
        public bool IsNestedPredicate;
        public readonly byte Tempvars;
        public Symbol[] Signature { get; internal set; }
        public bool IsTraced;

        #region Special predicates
        internal readonly Compiler.Compiler.PredicateImplementation PrimopImplementation;
        internal readonly Table Table;

        public bool IsSpecial => (PrimopImplementation != null || Table != null);

        #endregion

        #region Adding/removing compiled clauses
        internal static void AddClause(PredicateIndicator spec, CompiledClause compiledClause)
        {
            KB.Predicate(spec).AddClause(compiledClause);
        }

        internal void AddClause(CompiledClause compiledClause)
        {
            if (IsLocked)
                throw new InvalidOperationException("Attempt to add clause to read-only predicate "+Name);
            if (FirstClause == null)
                FirstClause = compiledClause;
            else
            {
                if (ExtraClauses == null)
                    ExtraClauses = new List<CompiledClause>();
                ExtraClauses.Add(compiledClause);
            }
        }

        public void Clear()
        {
            FirstClause = null;
            ExtraClauses = null;
            intConstants = null;
            floatConstants = null;
            objectConstants = null;
        }
        #endregion
        
        #region Constants for compiled code
        internal T GetObjectConstant<T>(byte index)
        {
            return (T) objectConstants[index];
        }

        internal int GetIntConstant(byte index)
        {
            return intConstants[index];
        }

        internal float GetFloatConstant(byte index)
        {
            return floatConstants[index];
        }

        internal byte GetObjectConstantIndex(object o)
        {
            if (objectConstants == null)
                objectConstants = new List<object>();
            var i = objectConstants.IndexOf(o);
            if (i >= 0)
                return (byte) i;
            objectConstants.Add(o);
            return (byte)(objectConstants.Count - 1);
        }

        internal byte GetIntConstantIndex(int n)
        {
            if (intConstants == null)
                intConstants = new List<int>();
            var i = intConstants.IndexOf(n);
            if (i >= 0)
                return (byte)i;
            intConstants.Add(n);
            return (byte)(intConstants.Count - 1);
        }

        internal byte GetFloatConstantIndex(float f)
        {
            if (floatConstants == null)
                floatConstants = new List<float>();
            var i = floatConstants.IndexOf(f);
            if (i >= 0)
                return (byte)i;
            floatConstants.Add(f);
            return (byte)(floatConstants.Count - 1);
        }

        internal float GetFloatConstant(OpcodeConstantType constantType, byte constantArg)
        {
            switch (constantType)
            {
                case OpcodeConstantType.Float:
                    return floatConstants[constantArg];

                case OpcodeConstantType.Integer:
                    return intConstants[constantArg];

                case OpcodeConstantType.SmallInteger:
                    return (sbyte) constantArg;

                default:
                    throw new InvalidOperationException("Attempt to get float constant from a non-floatable constant type");
            }
        }

        /// <summary>
        /// Called on a nested predicate to make it share the constant tables of the parent predicate.
        /// </summary>
        public void ImportConstantTablesFrom(Predicate parent)
        {
            intConstants = parent.intConstants;
            floatConstants = parent.floatConstants;
            objectConstants = parent.objectConstants;
        }
        #endregion

        public override string ToString()
        {
            return $"{Name}/{Arity}";
        }
    }
}
