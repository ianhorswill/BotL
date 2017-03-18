#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CodeBuilder.cs" company="Ian Horswill">
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

namespace BotL.Compiler
{
    /// <summary>
    /// Used to create a bytecode vector for a compiled rule.
    /// </summary>
    class CodeBuilder
    {
        public CodeBuilder(Predicate p)
        {
            Predicate = p;
        }

        private readonly List<byte> code = new List<byte>();
        public readonly Predicate Predicate;

        public byte[] Code => code.ToArray();

        public void Emit(Opcode op)
        {
            code.Add((byte)op);
        }

        public void Emit(FOpcode op)
        {
            code.Add((byte)op);
        }

        public void Emit(byte b)
        {
            code.Add(b);
        }

        public void EmitGoal(Predicate o)
        {
            Emit(Opcode.CGoal);
            code.Add(Predicate.GetObjectConstantIndex(o));
        }

        public void EmitConstant(Opcode headConst, object o)
        {
            Emit(headConst);
            if (o is int)
            {
                var i = (int) o;
                if (Math.Abs(i) < 128)
                {

                    Emit((byte)OpcodeConstantType.SmallInteger);
                    Emit((byte)i);
                }
                else
                {
                    Emit((byte) OpcodeConstantType.Integer);
                    Emit(Predicate.GetIntConstantIndex((int) o));
                }
            }
            else if (o is float)
            {
                Emit((byte)OpcodeConstantType.Float);
                Emit(Predicate.GetFloatConstantIndex((float)o));
            }
            else if (o is bool)
            {
                Emit((byte)OpcodeConstantType.Boolean);
                Emit((byte)(((bool)o)?1:0));
            }
            else
            {
                Emit((byte)OpcodeConstantType.Object);
                Emit(Predicate.GetObjectConstantIndex(o));
            }
        }

        public void EmitBuiltin(Builtin builtinOpcode)
        {
            Emit(Opcode.CBuiltin);
            Emit((byte)builtinOpcode);
        }
    }
}
