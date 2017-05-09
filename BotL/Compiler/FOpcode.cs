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
        /// <summary>
        /// Format: PushSmallInt SByte
        /// Pushes the specified integer on the stack.
        /// </summary>
        PushSmallInt,
        /// <summary>
        /// Format: PushInt Index
        /// Pushes a integer from the integer constant table.  Index is the position in the table.
        /// </summary>
        PushInt,
        /// <summary>
        /// Format: PushFloat Index
        /// Pushes a float from the float constant table.  Index is the position in the table.
        /// </summary>
        PushFloat,
        /// <summary>
        /// Format: PushBoolean Byte
        /// Pushes the specified boolean.  Byte must be 1 (true) or 0 (false)
        /// </summary>
        PushBoolean,
        /// <summary>
        /// Format: PushObject Index
        /// Pushes an object from the object constant table.  Index is the position in the table.
        /// </summary>
        PushObject,
        /// <summary>
        /// Fomat: Load Index
        /// Pushes the value of the variable from this rule's environment onto the stack.
        /// Index is the position in this rule's frame.  Variable may aliased, but not unbound.
        /// </summary>
        Load,
        /// <summary>
        /// Like load, but doesn't throw an exception if variable in uninstantiated.
        /// </summary>
        LoadUnchecked,
        /// <summary>
        /// Format: LoadGlobal Index
        /// Pushes the value of the specified global variable on the stack.  Index is the position of the
        /// global variable object itself in the object constant table.
        /// </summary>
        LoadGlobal,
        /// <summary>
        /// Format: Add
        /// Replaces the top two values on the stack with their sum
        /// </summary>
        Add,
        /// <summary>
        /// Format: Subtract
        /// Replaces the top two values on the stack with their difference
        /// </summary>
        Subtract,
        /// <summary>
        /// Format: Multiply
        /// Replaces the top two values on the stack with their product
        /// </summary>
        Multiply,
        /// <summary>
        /// Format: Divide
        /// Replaces the top two values on the stack with their quotient.
        /// Values are converted to floats before division.
        /// </summary>
        Divide,
        /// <summary>
        /// Format: Negate
        /// Replaces the top value on the stack with its inverse.
        /// </summary>
        Negate,
        /// <summary>
        /// Format: targetcode ... PushObject methodname argumentcode ... MethodCall ArgumentCount
        /// Calls method on target object with specified arguments and pushes the result.
        /// Arguments for the methodcall should be pushed in order, so that the first argument is pushed first.
        /// </summary>
        MethodCall,
        /// <summary>
        /// Format: tagetcode ... PushObject fieldname FieldReference
        /// Returns the value of the specified field of the specified target object.
        /// </summary>
        FieldReference,
        /// <summary>
        /// Format: targetcode ... PushObject ComponentType ComponentLookup
        /// Pushes the component of the target gameobject that has the specified type.
        /// </summary>
        ComponentLookup,
        /// <summary>
        /// Format: NonFalse
        /// Replaces the top of the stack with true if that value wasn't false.  Otherwise leaves it false.
        /// </summary>
        NonFalse,
        /// <summary>
        /// Format: Length
        /// Replaces the IList on top of the stack with its length.
        /// </summary>
        Length,
        /// <summary>
        /// Format: Array Length
        /// Creates an object[] array of the specified length, fills it with the top Length
        /// values of the stack, and pushes the array.
        /// Values on the stack should be pushed in order, so that the first element is pushed first.
        /// </summary>
        Array,
        /// <summary>
        /// Format: ArrayList Length
        /// Creates an ArrayList of the specified length, fills it with the top Length
        /// values of the stack, and pushes the ArrayList.
        /// Values on the stack should be pushed in REVERSE order, so that the first element is pushed ast.
        /// </summary>
        ArrayList,
        /// <summary>
        /// Format: Queue Length
        /// Creates a Queue of the specified length, fills it with the top Length
        /// values of the stack, and pushes the Queue object.
        /// Values on the stack should be pushed in REVERSE order, so that the first element is pushed ast.
        /// </summary>
        Queue,
        /// <summary>
        /// Format: Hashset Length
        /// Creates a Hashset of the specified length, fills it with the top Length
        /// values of the stack, and pushes the Hashset.
        /// </summary>
        Hashset,
        /// <summary>
        /// Format: PushObject Type argumentcode ... Constructor ArgumentCount
        /// Creates an object of the specified type and pushes it on the stack.
        /// Arguments for the methodcall should be pushed in order, so that the first argument is pushed first.
        /// </summary>
        Constructor,
        /// <summary>
        /// Replaces TOS with its absolute value
        /// </summary>
        Abs,
        /// <summary>
        /// Replaces TOS with its square root
        /// </summary>
        Sqrt,
        /// <summary>
        /// Replaces the top two elements of the stack with their power.
        /// Stack: X Y -> X^Y
        /// </summary>
        Pow,
        /// <summary>
        /// Replaces TOS with its log
        /// </summary>
        Log,
        /// <summary>
        /// Replaces TOS with e^TOS
        /// </summary>
        Exp,
        /// <summary>
        /// Replaces TOS with ceiling(TOS)
        /// </summary>
        Ceiling,
        /// <summary>
        /// Replaces TOS with floor(TOS)
        /// </summary>
        Floor,
        /// <summary>
        /// Replaces TOS with its sine
        /// </summary>
        Sin,
        /// <summary>
        /// Replaces TOS with its cosine
        /// </summary>
        Cos,
        /// <summary>
        /// Replaces TOS with its tangent
        /// </summary>
        Tan,
        /// <summary>
        /// Replaces TOS with its arctangent
        /// </summary>
        Atan,
        /// <summary>
        /// Replaces the top two elements of the stack with the arctangent of their quotient.
        /// Stack: X Y -> atan2(X,Y)
        /// </summary>
        Atan2,
        /// <summary>
        /// Replaces the two two elements of the stack with the smaller of the two.
        /// </summary>
        Min,
        /// <summary>
        /// Replaces the two two elements of the stack with the larger of the two.
        /// </summary>
        Max,
        /// <summary>
        /// World-space between two GameObjects
        /// </summary>
        Distance,
        /// <summary>
        /// Squared world-space distance between two game objects.
        /// </summary>
        DistanceSq,
        /// <summary>
        /// Does run-time name resolution of named entities ($ is compile-time).
        /// </summary>
        ResolveName,
        /// <summary>
        /// Format: RandomInt
        /// Replaces the top two values on the stack with a random integer between the first and the second.
        /// </summary>
        RandomInt,
        /// <summary>
        /// Format: RandomFloat
        /// Replaces the top two values on the stack with a random integer between the first and the second.
        /// </summary>
        RandomFloat,
        /// <summary>
        /// The CLR string.Format method.
        /// </summary>
        Format,
        /// <summary>
        /// Marks the end of a functional expression
        /// </summary>
        Return = 255
    }

    internal static class FOpcodeTable
    {
        static readonly Dictionary<PredicateIndicator, FOpcode> OpcodeTable = new Dictionary<PredicateIndicator, FOpcode>();

        internal static bool ReverseArguments(FOpcode o)
        {
            return o == FOpcode.ArrayList || o == FOpcode.Queue;
        }

        internal static FOpcode Opcode(Call c)
        {
            FOpcode result;
            if (OpcodeTable.TryGetValue(new PredicateIndicator(c), out result))
                return result;
            if (c.Functor == Symbol.Array)
                return FOpcode.Array;
            if (c.Functor == Symbol.ArrayList)
                return FOpcode.ArrayList;
            if (c.Functor == Symbol.Queue)
                return FOpcode.Queue;
            if (c.Functor == Symbol.Hashset)
                return FOpcode.Hashset;
            if (c.Functor == Symbol.Format && c.Arity > 0)
                return FOpcode.Format;;
            throw new Exception($"{c}, an unknown functional expression, appears as an argument in {Compiler.Compiler.CurrentGoal}.");
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
            OpcodeTable[new PredicateIndicator("length", 1)] = FOpcode.Length;

            OpcodeTable[new PredicateIndicator("abs", 1)] = FOpcode.Abs;
            OpcodeTable[new PredicateIndicator("sqrt", 1)] = FOpcode.Sqrt;
            OpcodeTable[new PredicateIndicator("pow", 2)] = FOpcode.Pow;
            OpcodeTable[new PredicateIndicator("log", 1)] = FOpcode.Log;
            OpcodeTable[new PredicateIndicator("exp", 1)] = FOpcode.Exp;
            OpcodeTable[new PredicateIndicator("ceiling", 1)] = FOpcode.Ceiling;
            OpcodeTable[new PredicateIndicator("floor", 1)] = FOpcode.Floor;
            OpcodeTable[new PredicateIndicator("sin", 1)] = FOpcode.Sin;
            OpcodeTable[new PredicateIndicator("cos", 1)] = FOpcode.Cos;
            OpcodeTable[new PredicateIndicator("tan", 1)] = FOpcode.Tan;
            OpcodeTable[new PredicateIndicator("atan", 1)] = FOpcode.Atan;
            OpcodeTable[new PredicateIndicator("atan", 2)] = FOpcode.Atan2;

            OpcodeTable[new PredicateIndicator("min", 2)] = FOpcode.Min;
            OpcodeTable[new PredicateIndicator("max", 2)] = FOpcode.Max;

            OpcodeTable[new PredicateIndicator("distance", 2)] = FOpcode.Distance;
            OpcodeTable[new PredicateIndicator("distance_squared", 2)] = FOpcode.DistanceSq;
            OpcodeTable[new PredicateIndicator("#", 1)] = FOpcode.ResolveName;

            OpcodeTable[new PredicateIndicator("random_integer", 2)] = FOpcode.RandomInt;
            OpcodeTable[new PredicateIndicator("random_float", 2)] = FOpcode.RandomFloat;
        }
    }
}
