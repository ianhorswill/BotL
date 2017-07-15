#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Structs.cs" company="Ian Horswill">
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
using System.Linq;

namespace BotL.Compiler
{
    /// <summary>
    /// Table of declared struct types
    /// </summary>
    public static class Structs
    {
        public static readonly object PaddingValue = System.Reflection.Missing.Value;
        static readonly Dictionary<Symbol, Symbol[]> StructSlots = new Dictionary<Symbol, Symbol[]>();

        internal static void DeclareStruct(object shape)
        {
            var c = shape as Call;
            if (c == null)
                throw new SyntaxError("Malformed struct declaration", shape);
            StructSlots[c.Functor] = c.Arguments.Cast<Symbol>().ToArray();
        }

        public static void FlattenInto(object o, Symbol type, List<object> destination)
        {
            Symbol[] slots;
            if (!StructSlots.TryGetValue(type, out slots))
                throw new ArgumentException("Unknown struct name: " + type);
            var size = slots.Length;
            if (Variable.IsVariableName(o))
            {
                FlattenVariable((Symbol)o, slots, destination);
            }
            else
            {
                var c = o as Call;
                if (c != null && c.Functor == type)
                {
                    if (c.Arity == 0)
                    {
                        // Pad with anonymous variables
                        for (var i = 0; i < size; i++)
                            destination.Add(Symbol.Underscore);
                    }
                    else if (c.Arity == 1)
                    {
                        FlattenVariable((Symbol) c.Arguments[0], slots, destination);
                    }
                    else if (c.Arity == size)
                    {
                        foreach (var a in c.Arguments)
                            destination.Add(a);
                    }
                    else
                        throw new SyntaxError("Malformed struct expression", o);
                }
                else if (c != null && (c.IsFunctor(Symbol.DollarSign, 1) || c.IsFunctor(Symbol.Hash, 1)))
                    destination.Add(c);
                else
                    for (var pad = FlattenInto(o, size, destination); pad > 0; pad--)
                        destination.Add(PaddingValue);
            }
        }

        private static void FlattenVariable(Symbol pseudoVarName, Symbol[] slots, List<object> destination)
        {
            if (pseudoVarName == Symbol.Underscore)
                for (var i = 0; i < slots.Length; i++)
                {
                    destination.Add(pseudoVarName);
                }
            else
            {
                var prefix = pseudoVarName.Name + ".";
                foreach (var a in slots)
                    destination.Add(Symbol.Intern(prefix + a.Name));
            }
        }

        private static int FlattenInto(object o, int remainingSize, List<object> destination)
        {
            if (remainingSize<1)
                throw new ArgumentException("Argument is too large for struct");
            var c = o as Call;
            if (c == null || c.IsFunctor(Symbol.DollarSign, 1) || c.IsFunctor(Symbol.Hash, 1))
            {
                destination.Add(o);
                return remainingSize - 1;
            }
            destination.Add(c.Functor);
            remainingSize -= 1;
            foreach (var a in c.Arguments)
                remainingSize = FlattenInto(a, remainingSize, destination);
            return remainingSize;
        }
    }
}
