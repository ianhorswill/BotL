#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FOpcode.cs" company="Ian Horswill">
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
    /// Opcodes for functional expressions in byte compiled clauses
    /// </summary>
    public enum FOpcode
    {
        PushSmallInt,
        PushInt,
        PushFloat,
        PushBoolean,
        PushObject,
        Load,
        LoadGlobal,
        Add,
        Subtract,
        Multiply,
        Divide,
        Negate,
        MethodCall,
        FieldReference,
        ComponentLookup,
        NonFalse,
        Array,
        ArrayList,
        Hashset,
        Constructor,
        Return = 255
    }

    internal static class FOpcodeTable
    {
        static readonly Dictionary<PredicateIndicator, FOpcode> OpcodeTable = new Dictionary<PredicateIndicator, FOpcode>();

        internal static FOpcode Opcode(Call c)
        {
            FOpcode result;
            if (OpcodeTable.TryGetValue(new PredicateIndicator(c), out result))
                return result;
            if (c.Functor == Symbol.Array)
                return FOpcode.Array;
            if (c.Functor == Symbol.ArrayList)
                return FOpcode.ArrayList;
            if (c.Functor == Symbol.Hashset)
                return FOpcode.Hashset;
            throw new Exception("Unknown functional expression operation in: "+c);
        }

        static FOpcodeTable()
        {
            OpcodeTable[new PredicateIndicator("+", 2)] = FOpcode.Add;
            OpcodeTable[new PredicateIndicator("-", 2)] = FOpcode.Subtract;
            OpcodeTable[new PredicateIndicator("-", 1)] = FOpcode.Negate;
            OpcodeTable[new PredicateIndicator("*", 2)] = FOpcode.Multiply;
            OpcodeTable[new PredicateIndicator("/", 2)] = FOpcode.Divide;
            OpcodeTable[new PredicateIndicator("non_false", 1)] = FOpcode.NonFalse;
            OpcodeTable[new PredicateIndicator("new", 1)] = FOpcode.Constructor;
            OpcodeTable[new PredicateIndicator("::", 2)] = FOpcode.ComponentLookup;
        }
    }
}
