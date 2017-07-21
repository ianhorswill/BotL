#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FunctionalExpression.cs" company="Ian Horswill">
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
using System.Collections.Generic;
using BotL.Unity;
using UnityEngine;
using static BotL.Engine;

namespace BotL
{
    /// <summary>
    /// Evaluator for compiled functional expressions.
    /// Functional expressions have their own byte codes (FOpcode), but these are embedded
    /// in the normal byte code.
    /// </summary>
    internal static class FunctionalExpression
    {
        public const ushort EvalStackOffset = 256;
        private static readonly System.Random Random = new System.Random();

        /// <summary>
        /// Execute the compiled functional express starting at pc inside of clause.
        /// Uses the area of the data stack starting EvalStackOffset entries after dTop.
        /// On exit, the result will be in Engine.DataStack[dTop+EvalStackOffset]
        /// </summary>
        /// <param name="p">Predicate being executed (for access to constant tables)</param>
        /// <param name="clause">Compiled clause code</param>
        /// <param name="goalFrame">The frame of the predicate evaluating this functional expression.</param>
        /// <param name="frameBase">Base address of clause's environment in DataStack</param>
        /// <param name="pc">PC within clause</param>
        /// <param name="stack">top of the data stack</param>
        /// <returns>PC of the next instruction after the functional expression</returns>
        public static ushort Eval(Predicate p, byte[] clause, ushort pc, ushort goalFrame, ushort frameBase, ushort stack)
        {
            // The top of the stack is actually at 
            stack += EvalStackOffset;

            while (true)
            {
                Profiler.MaybeSampleStack(goalFrame);
                var fOpcode = (FOpcode) clause[pc++];
                switch (fOpcode)
                {
                    case FOpcode.Return:
                        return pc;

                    case FOpcode.PushSmallInt:
                        DataStack[stack++].Set((sbyte) clause[pc++]);
                        break;

                    case FOpcode.PushInt:
                        DataStack[stack++].Set(p.GetIntConstant(clause[pc++]));
                        break;

                    case FOpcode.PushFloat:
                        DataStack[stack++].Set(p.GetFloatConstant(clause[pc++]));
                        break;

                    case FOpcode.PushBoolean:
                        DataStack[stack++].Set(clause[pc++] != 0);
                        break;

                    case FOpcode.PushObject:
                        DataStack[stack++].SetReference(p.GetObjectConstant<object>(clause[pc++]));
                        break;

                    case FOpcode.Load:
                    case FOpcode.LoadUnchecked:
                        var address = Deref(frameBase + clause[pc++]);
                        if (DataStack[address].Type == TaggedValueType.Unbound && fOpcode == FOpcode.Load)
                            throw new InstantiationException("Uninstantiated variable in functional expression");
                        DataStack[stack++] = DataStack[address];
                        break;

                    case FOpcode.LoadGlobal:
                        var globalVariable = (GlobalVariable) p.GetObjectConstant<object>(clause[pc++]);
                        DataStack[stack++] = globalVariable.Value;
                        break;

                    case FOpcode.Add:
                    {
                        var op2Addr = --stack;
                        var op1Addr = --stack;
                        if (BothInts(op1Addr, op2Addr))
                            DataStack[stack++].Set(DataStack[op1Addr].integer +
                                                   DataStack[op2Addr].integer);
                        else
                            DataStack[stack++].Set(DataStack[op1Addr].AsFloat +
                                                   DataStack[op2Addr].AsFloat);
                    }
                        break;

                    case FOpcode.Subtract:
                    {
                        var op2Addr = --stack;
                        var op1Addr = --stack;
                        if (BothInts(op1Addr, op2Addr))
                            DataStack[stack++].Set(DataStack[op1Addr].integer -
                                                   DataStack[op2Addr].integer);
                        else
                            DataStack[stack++].Set(DataStack[op1Addr].AsFloat -
                                                   DataStack[op2Addr].AsFloat);
                    }
                        break;

                    case FOpcode.Negate:
                    {
                        var op1Addr = --stack;
                        if (DataStack[op1Addr].Type == TaggedValueType.Integer)
                            DataStack[stack++].Set(-DataStack[op1Addr].integer);
                        else
                            DataStack[stack++].Set(DataStack[op1Addr].floatingPoint);
                    }
                        break;

                    case FOpcode.Multiply:
                    {
                        var op2Addr = --stack;
                        var op1Addr = --stack;
                        if (BothInts(op1Addr, op2Addr))
                            DataStack[stack++].Set(DataStack[op1Addr].integer *
                                                   DataStack[op2Addr].integer);
                        else
                            DataStack[stack++].Set(DataStack[op1Addr].AsFloat *
                                                   DataStack[op2Addr].AsFloat);
                    }
                        break;

                    case FOpcode.Divide:
                    {
                        var op2Addr = --stack;
                        var op1Addr = --stack;
                        DataStack[stack++].Set(DataStack[op1Addr].AsFloat /
                                               DataStack[op2Addr].AsFloat);
                    }
                        break;

                    case FOpcode.FieldReference:
                    {
                        var fieldName = (string) DataStack[--stack].reference;
                        var target = DataStack[--stack].Value;
                        var result = target.GetPropertyOrField(fieldName);
                        DataStack[stack++].SetGeneral(result);
                    }
                        break;

                    case FOpcode.MethodCall:
                    {
                        object[] args = new object[clause[pc++]];
                        for (var i = args.Length - 1; i >= 0; i--)
                            args[i] = DataStack[--stack].Value;
                        var methodName = (string) DataStack[--stack].reference;
                        var target = DataStack[--stack].Value;
                        var result = target.InvokeMethod(methodName, args);
                        DataStack[stack++].SetGeneral(result);
                    }
                        break;

                    case FOpcode.Constructor:
                    {
                        object[] args = new object[clause[pc++]];
                        for (var i = args.Length - 1; i >= 0; i--)
                            args[i] = DataStack[--stack].Value;
                        var type = (Type) DataStack[--stack].reference;
                        var result = type.CreateInstance(args);
                        DataStack[stack++].SetGeneral(result);
                    }
                        break;

                    case FOpcode.ComponentLookup:
                    {
                        stack = UnityUtilities.LookupUnityComponent(stack);
                    }
                        break;

                    case FOpcode.Length:
                    {
                        var addr = stack - 1;
                        var collection = DataStack[addr].Value as ICollection;
                        if (collection == null)
                            throw new ArgumentTypeException("length", 1, "Argument is not a collection",
                                DataStack[addr].Value);
                        DataStack[addr].Set(collection.Count);
                    }
                        break;

                    case FOpcode.Item:
                    {
                        var indexAddr = --stack;
                        if (DataStack[indexAddr].Type != TaggedValueType.Integer)
                            throw new ArgumentTypeException("item", 2, "Index argument is not an integer",
                                DataStack[indexAddr].Value);
                        var listAddr = --stack;
                        var list = DataStack[listAddr].reference as IList;
                        if (DataStack[listAddr].Type != TaggedValueType.Reference || list == null)
                            throw new ArgumentTypeException("item", 1, "List argument is not an list",
                                DataStack[indexAddr].Value);
                        DataStack[stack++].SetGeneral(list[DataStack[indexAddr].integer]);
                    }
                        break;

                    case FOpcode.NodeKey:
                        {
                            var addr = stack - 1;
                            var elNode = DataStack[addr].Value as ELNode;
                            if (elNode == null)
                                throw new ArgumentTypeException("node_key", 1, "Argument is not an EL node",
                                    DataStack[addr].Value);
                            DataStack[addr] = elNode.Key;  // This is a tagged value so we just copy it.
                        }
                        break;

                    case FOpcode.NodeParent:
                        {
                            var addr = stack - 1;
                            var elNode = DataStack[addr].Value as ELNode;
                            if (elNode == null)
                                throw new ArgumentTypeException("node_parent", 1, "Argument is not an EL node",
                                    DataStack[addr].Value);
                            DataStack[addr].SetReference(elNode.Parent);
                        }
                        break;

                    case FOpcode.Array:
                    {
                        var result = new object[clause[pc++]];
                        for (int i = result.Length - 1; i >= 0; i--)
                            result[i] = DataStack[--stack].Value;
                        DataStack[stack++].SetReference(result);
                    }
                        break;

                    case FOpcode.ArrayList:
                    {
                        var capacity = clause[pc++];
                        var result = new ArrayList(capacity);
                        for (int i = 0; i < capacity; i++)
                            result.Add(DataStack[--stack].Value);
                        DataStack[stack++].SetReference(result);
                    }
                        break;

                    case FOpcode.Queue:
                    {
                        var capacity = clause[pc++];
                        var result = new Queue();
                        for (int i = 0; i < capacity; i++)
                            result.Enqueue(DataStack[--stack].Value);
                        DataStack[stack++].SetReference(result);
                    }
                        break;

                    case FOpcode.Hashset:
                    {
                        var count = clause[pc++];
                        var result = new HashSet<object>();
                        for (int i = 0; i < count; i++)
                            result.Add(DataStack[--stack].Value);
                        DataStack[stack++].SetReference(result);
                    }
                        break;

                    case FOpcode.NonFalse:
                        // Push true on the stack if TOS is anything but the boolean False.
                    {
                        var tos = stack - 1;
                        DataStack[tos].Set(DataStack[tos].Type != TaggedValueType.Boolean || DataStack[tos].boolean);
                    }
                        break;

                    case FOpcode.Min:
                    {
                        var op2Addr = --stack;
                        var op1Addr = --stack;
                        if (BothInts(op1Addr, op2Addr))
                            DataStack[stack++].Set(Math.Min(DataStack[op1Addr].integer,
                                DataStack[op2Addr].integer));
                        else
                            DataStack[stack++].Set(Mathf.Min(DataStack[op1Addr].AsFloat,
                                DataStack[op2Addr].AsFloat));
                    }
                        break;

                    case FOpcode.Max:
                    {
                        var op2Addr = --stack;
                        var op1Addr = --stack;
                        if (BothInts(op1Addr, op2Addr))
                            DataStack[stack++].Set(Math.Max(DataStack[op1Addr].integer,
                                DataStack[op2Addr].integer));
                        else
                            DataStack[stack++].Set(Mathf.Max(DataStack[op1Addr].AsFloat,
                                DataStack[op2Addr].AsFloat));
                    }
                        break;

                    case FOpcode.Abs:
                    {
                        var op1Addr = --stack;
                        if (DataStack[op1Addr].Type == TaggedValueType.Integer)
                            DataStack[stack++].Set(Math.Abs(DataStack[op1Addr].integer));
                        else
                            DataStack[stack++].Set(Mathf.Abs(DataStack[op1Addr].floatingPoint));
                    }
                        break;

                    case FOpcode.Ceiling:
                    {
                        var addr = stack - 1;
                        DataStack[addr].Set(Mathf.Ceil(DataStack[addr].AsFloat));
                    }
                        break;

                    case FOpcode.Floor:
                    {
                        var addr = stack - 1;
                        DataStack[addr].Set(Mathf.Floor(DataStack[addr].AsFloat));
                    }
                        break;

                    case FOpcode.Sqrt:
                    {
                        var addr = stack - 1;
                        DataStack[addr].Set(Mathf.Sqrt(DataStack[addr].AsFloat));
                    }
                        break;

                    case FOpcode.Log:
                    {
                        var addr = stack - 1;
                        DataStack[addr].Set(Mathf.Log(DataStack[addr].AsFloat));
                    }
                        break;

                    case FOpcode.Exp:
                    {
                        var addr = stack - 1;
                        DataStack[addr].Set(Mathf.Exp(DataStack[addr].AsFloat));
                    }
                        break;

                    case FOpcode.Sin:
                    {
                        var addr = stack - 1;
                        DataStack[addr].Set(Mathf.Sin(DataStack[addr].AsFloat));
                    }
                        break;

                    case FOpcode.Cos:
                    {
                        var addr = stack - 1;
                        DataStack[addr].Set(Mathf.Cos(DataStack[addr].AsFloat));
                    }
                        break;

                    case FOpcode.Tan:
                    {
                        var addr = stack - 1;
                        DataStack[addr].Set(Mathf.Tan(DataStack[addr].AsFloat));
                    }
                        break;

                    case FOpcode.Atan:
                    {
                        var addr = stack - 1;
                        DataStack[addr].Set(Mathf.Atan(DataStack[addr].AsFloat));
                    }
                        break;

                    case FOpcode.Atan2:
                    {
                        var op2Addr = --stack;
                        var op1Addr = --stack;
                        DataStack[stack++].Set(Mathf.Atan2(DataStack[op1Addr].AsFloat,
                            DataStack[op2Addr].AsFloat));
                    }
                        break;

                    case FOpcode.Pow:
                    {
                        var op2Addr = --stack;
                        var op1Addr = --stack;
                        DataStack[stack++].Set(Mathf.Pow(DataStack[op1Addr].AsFloat,
                            DataStack[op2Addr].AsFloat));
                    }
                        break;

                    case FOpcode.Distance:
                        var distance = UnityUtilities.Distance(DataStack[--stack].Value, DataStack[--stack].Value);
                        DataStack[stack++].Set(distance);
                        break;

                    case FOpcode.DistanceSq:
                        var distanceSq = UnityUtilities.DistanceSq(DataStack[--stack].Value, DataStack[--stack].Value);
                        DataStack[stack++].Set(distanceSq);
                        break;

                    case FOpcode.ResolveName:
                    {
                        var result = NamedEntities.Resolve(DataStack[--stack].Value);
                        DataStack[stack++].SetGeneral(result);
                        break;
                    }

                    case FOpcode.RandomInt:
                    {
                        if (DataStack[--stack].Type != TaggedValueType.Integer)
                            throw new ArgumentTypeException("random_integer", 2, "Should be an integer",
                                DataStack[stack].Value);
                        var upperBound = DataStack[stack].integer;
                        if (DataStack[--stack].Type != TaggedValueType.Integer)
                            throw new ArgumentTypeException("random_integer", 1, "Should be an integer",
                                DataStack[stack].Value);
                        var lowerBound = DataStack[stack].integer;
                        DataStack[stack++].Set(Random.Next(lowerBound, upperBound));
                        break;
                    }

                    case FOpcode.RandomFloat:
                    {
                        --stack;
                        if (DataStack[stack].Type != TaggedValueType.Integer &&
                            DataStack[stack].Type != TaggedValueType.Float)
                            throw new ArgumentTypeException("random_float", 2, "Should be a number",
                                DataStack[stack].Value);
                        var upperBound = DataStack[stack].AsFloat;

                        --stack;
                        if (DataStack[stack].Type != TaggedValueType.Integer &&
                            DataStack[stack].Type != TaggedValueType.Float)
                            throw new ArgumentTypeException("random_float", 1, "Should be a number",
                                DataStack[stack].Value);
                        var lowerBound = DataStack[stack].AsFloat;

                        DataStack[stack++].Set(lowerBound + (upperBound-lowerBound)*(float)Random.NextDouble());
                        break;
                    }

                    case FOpcode.Format:
                        {
                            object[] args = new object[clause[pc++]-1];
                            for (var i = args.Length - 1; i >= 0; i--)
                                args[i] = DataStack[--stack].Value;
                            var formatString = (string)DataStack[--stack].Value;
                            DataStack[stack++].SetGeneral(string.Format(formatString, args));
                            break;
                        }

                    case FOpcode.UserFunction:
                    {
                        stack = UserFunction.UserFunctions[clause[pc++]].Run(stack);
                        break;
                    }

                    default:
                        throw new InvalidOperationException("Bad opcode in compiled functional expression");
                }
            }
        }

        private static bool BothInts(ushort op1Addr, ushort op2Addr)
        {
            return DataStack[op1Addr].Type == TaggedValueType.Integer &&
                   DataStack[op2Addr].Type == TaggedValueType.Integer;
        }
    }
}
