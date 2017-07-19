#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Primops.cs" company="Ian Horswill">
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
using BotL.Compiler;
using static BotL.KB;
using static BotL.Engine;

namespace BotL
{
    /// <summary>
    /// Definitions related to primitive predicates
    /// </summary>
    public static class Primops
    {
        /// <summary>
        /// Add the built-in primops to the KB.
        /// </summary>
        internal static void DefinePrimops()
        {
            Table.DefineTablePrimops();
            ELNode.DefineELPrimops();
            Queue.DefineQueuePrimops();

            #region Equality testing
            // Nonunifiability test
            DefinePrimop("!=", 2, (argBase, ignore) =>
            {
                var addr1 = Deref(argBase);
                var addr2 = Deref(argBase + 1);
                if (DataStack[addr1].Type == TaggedValueType.Unbound || DataStack[addr2].Type == TaggedValueType.Unbound)
                    return CallStatus.Fail;
                if (DataStack[addr1].Type != DataStack[addr2].Type)
                    return CallStatus.Fail;
                switch (DataStack[addr1].Type)
                {
                    case TaggedValueType.Boolean:
                        return (DataStack[addr1].boolean != DataStack[addr2].boolean)?CallStatus.DeterministicSuccess : CallStatus.Fail;

                    case TaggedValueType.Integer:
                        return (DataStack[addr1].integer != DataStack[addr2].integer) ? CallStatus.DeterministicSuccess : CallStatus.Fail;

                    case TaggedValueType.Float:
                        // ReSharper disable once CompareOfFloatsByEqualityOperator
                        return (DataStack[addr1].floatingPoint != DataStack[addr2].floatingPoint) ? CallStatus.DeterministicSuccess : CallStatus.Fail;

                    default:
                        return (!Equals(DataStack[addr1].reference, DataStack[addr2].reference)) ? CallStatus.DeterministicSuccess : CallStatus.Fail;
                }
            }, mandatoryInstatiation: true, semiDeterministic: true);
            #endregion

            #region Numerical operations
            // Numerical comparisons
            DefinePrimop(">", 2, (argBase, ignore) => (DataStack[Deref(argBase)].AsFloat > DataStack[Deref(argBase + 1)].AsFloat) ? CallStatus.DeterministicSuccess : CallStatus.Fail,
                mandatoryInstatiation: true, semiDeterministic: true);
            DefinePrimop(">=", 2, (argBase, ignore) => (DataStack[Deref(argBase)].AsFloat >= DataStack[Deref(argBase + 1)].AsFloat) ? CallStatus.DeterministicSuccess : CallStatus.Fail,
                mandatoryInstatiation: true, semiDeterministic: true);
            DefinePrimop("<", 2, (argBase, ignore) => (DataStack[Deref(argBase)].AsFloat < DataStack[Deref(argBase + 1)].AsFloat) ? CallStatus.DeterministicSuccess : CallStatus.Fail,
                mandatoryInstatiation: true, semiDeterministic: true);
            DefinePrimop("=<", 2, (argBase, ignore) => (DataStack[Deref(argBase)].AsFloat <= DataStack[Deref(argBase + 1)].AsFloat) ? CallStatus.DeterministicSuccess : CallStatus.Fail,
                mandatoryInstatiation: true, semiDeterministic: true);
            #endregion

            #region Type testing
            // Binding tests
            DefinePrimop("var", 1, (argBase, ignore) => (DataStack[Deref(argBase)].Type == TaggedValueType.Unbound) ? CallStatus.DeterministicSuccess : CallStatus.Fail,
                semiDeterministic: true);
            DefinePrimop("nonvar", 1, (argBase, ignore) => (DataStack[Deref(argBase)].Type != TaggedValueType.Unbound) ? CallStatus.DeterministicSuccess : CallStatus.Fail,
                semiDeterministic: true);

            // Type tests
            DefinePrimop("integer", 1, (argBase, ignore) => (DataStack[Deref(argBase)].Type == TaggedValueType.Integer) ? CallStatus.DeterministicSuccess : CallStatus.Fail,
                semiDeterministic: true);
            DefinePrimop("float", 1, (argBase,ignore) => (DataStack[Deref(argBase)].Type == TaggedValueType.Float) ? CallStatus.DeterministicSuccess : CallStatus.Fail,
                semiDeterministic: true);
            DefinePrimop("number", 1, (argBase, ignore) =>
            {
                var addr = Deref(argBase);
                return (DataStack[addr].Type == TaggedValueType.Float ||
                       DataStack[addr].Type == TaggedValueType.Integer) ? CallStatus.DeterministicSuccess : CallStatus.Fail;
            },
                semiDeterministic: true);
            DefinePrimop("symbol", 1, (argBase, ignore) =>
            {
                var addr = Deref(argBase);
                return (DataStack[addr].Type == TaggedValueType.Reference &&
                       DataStack[addr].reference != null &&
                       DataStack[addr].reference is Symbol) ? CallStatus.DeterministicSuccess : CallStatus.Fail;
            },
                semiDeterministic: true);
            DefinePrimop("string", 1, (argBase, ignore) =>
            {
                var addr = Deref(argBase);
                return (DataStack[addr].Type == TaggedValueType.Reference &&
                       DataStack[addr].reference != null &&
                       DataStack[addr].reference is string) ? CallStatus.DeterministicSuccess : CallStatus.Fail;
            },
                semiDeterministic: true);

            DefinePrimop("missing", 1, (argBase, ignore) =>
            {
                var addr = Deref(argBase);
                return (DataStack[addr].Type == TaggedValueType.Reference &&
                        DataStack[addr].reference == Structs.PaddingValue)
                    ? CallStatus.DeterministicSuccess
                    : CallStatus.Fail;
            },
                semiDeterministic: true);
            #endregion

            #region IO
            DefinePrimop("write", 1, (argBase, ignore) =>
            {
                Repl.StandardOutput.Write(DataStack[Deref(argBase)]);
                return CallStatus.DeterministicSuccess;
            },
            deterministic: true);

            DefinePrimop("write_line", 1, (argBase, ignore) =>
            {
                Repl.StandardOutput.WriteLine(DataStack[Deref(argBase)]);
                return CallStatus.DeterministicSuccess;
            },
            deterministic: true);
            #endregion

            #region Debugging support
#if !DEBUG
            DefinePrimop("log", 1, (argBase, ignore) =>
            {
                var value = DataStack[Deref(argBase)].Value;
                var debugString = value as string;
                if (debugString == null)
                    debugString = Parser.ExpressionParser.WriteExpressionToString(value);
                UnityEngine.Debug.Log(debugString);
                return CallStatus.DeterministicSuccess;
            },
            deterministic: true);
#endif
            #endregion

            #region Environment-related stuff
            DefinePrimop("load", 1, (argBase, ignore) =>
            {
                var addr = Deref(argBase);
                var path = DataStack[addr].reference as string;
                if (DataStack[addr].Type == TaggedValueType.Reference && path != null)
                {
                    Compiler.Compiler.CompileFile(path);
                }
                else
                    throw new ArgumentException("Argument to load must be a string");
                return CallStatus.DeterministicSuccess;
            },
            mandatoryInstatiation: true, deterministic: true);

            DefinePrimop("load_table", 1, (argBase, ignore) =>
            {
                var addr = Deref(argBase);
                var path = DataStack[addr].reference as string;
                if (DataStack[addr].Type == TaggedValueType.Reference && path != null)
                {
                    LoadTable(path);
                }
                else
                    throw new ArgumentException("Argument to load_table must be a string");
                return CallStatus.DeterministicSuccess;
            },
            mandatoryInstatiation: true, deterministic: true);
#endregion

#region C# interop

            DefinePrimop("set_property!", 3, (argBase, ignore) =>
            {
                var oAddr = Deref(argBase);
                if (DataStack[oAddr].Type != TaggedValueType.Reference)
                    throw new ArgumentTypeException("set_property", 1, "Argument should be a reference type",
                        DataStack[oAddr].ValueOrUnbound);
                var objArg = DataStack[oAddr].reference;
                var nameAddr = Deref(argBase + 1);
                if (DataStack[nameAddr].Type != TaggedValueType.Reference)
                    throw new ArgumentTypeException("set_property", 1, "Argument should be a string or symbol",
                        DataStack[nameAddr].ValueOrUnbound);
                var nArg = DataStack[nameAddr].reference;
                var name = nArg as string;
                if (name == null)
                {
                    var s = nArg as Symbol;
                    if (s != null)
                        name = s.Name;
                    else
                        throw new ArgumentTypeException("set_property", 1, "Argument should be a string or symbol",
                            DataStack[nameAddr].ValueOrUnbound);
                }
                objArg.SetPropertyOrField(name, DataStack[Deref(argBase + 2)].Value);
                return CallStatus.DeterministicSuccess;
            },
            mandatoryInstatiation: true, deterministic: true);

            DefinePrimop("in", 2, (argBase, restartCount) =>
            {
                var memberAddr = Deref(argBase);
                var collectionAddr = Deref(argBase + 1);
                var enumeratorAddr = argBase + 2;

                if (DataStack[collectionAddr].Type != TaggedValueType.Reference)
                {
                    throw new ArgumentException("Invalid collecction argument to in/2.");
                }

                var collection = DataStack[collectionAddr].Value;
                if (DataStack[memberAddr].Type == TaggedValueType.Unbound)
                {
                    // We're enumerating
                    var ilist = collection as IList;
                    if (ilist != null)
                    {
                        if (ilist.Count==0)
                            return CallStatus.Fail;
                        // Enumerating an IList
                        DataStack[memberAddr].SetGeneral(ilist[restartCount]);
                        SaveVariable(memberAddr);
                        return restartCount < ilist.Count-1
                            ? CallStatus.NonDeterministicSuccess
                            : CallStatus.DeterministicSuccess;
                    }
                    else
                    {
                        // Enumerating some other IEnumerable
                        if (restartCount == 0)
                        {
                            var ienumerable = collection as IEnumerable;
                            if (ienumerable == null)
                                throw new ArgumentException("Invalid collecction argument to in/2.");
                            DataStack[enumeratorAddr].reference = ienumerable.GetEnumerator();
                        }
                        var enumerator = (IEnumerator) DataStack[enumeratorAddr].reference;
                        if (enumerator.MoveNext())
                        {
                            DataStack[memberAddr].SetGeneral(enumerator.Current);
                            SaveVariable(memberAddr);
                            return CallStatus.NonDeterministicSuccess;
                        }
                        else return CallStatus.Fail;
                    }
                }
                else
                {
                    // We're testing membership
                    var member = DataStack[memberAddr].Value;
                    var hashset = collection as HashSet<object>;
                    if (hashset != null)
                        return hashset.Contains(member) ? CallStatus.DeterministicSuccess : CallStatus.Fail;
                    var ilist = collection as IList;
                    if (ilist != null)
                        return ilist.Contains(member) ? CallStatus.DeterministicSuccess : CallStatus.Fail;
                    var ienumerable = collection as IEnumerable;
                    if (ienumerable != null)
                    {
                        foreach (var e in ienumerable)
                            if (Equals(e, member))
                                return CallStatus.DeterministicSuccess;
                        return CallStatus.Fail;
                    }
                    throw new ArgumentException("Invalid collecction argument to in/2.");
                }
            },
            tempVars: 1);

            DefinePrimop("adjoin!", 2, (argBase, ignore) =>
            {
                var addr = Deref(argBase);
                if (DataStack[addr].Type == TaggedValueType.Unbound)
                    throw new InstantiationException("collection argument to adjoin must be instantiated");
                var collection = DataStack[addr].Value;
                addr = Deref(argBase + 1);
                if (DataStack[addr].Type == TaggedValueType.Unbound)
                    throw new InstantiationException("element argument to adjoin must be instantiated");
                var element = DataStack[addr].Value;

                var set = collection as HashSet<object>;
                if (set != null)
                {
                    set.Add(element);
                }
                else
                {
                    var list = collection as IList;
                    if (list != null)
                    {
                        list.Add(element);
                    }
                    else
                    {
                        throw new ArgumentException("Collection argument to adjust is not a list or hashset.");
                    }
                }

                return CallStatus.DeterministicSuccess;
            },
            mandatoryInstatiation: true, deterministic: true);

            DefinePrimop("item", 3, (argBase, restartCount) =>
            {
                var listAddr = Deref(argBase);
                if (DataStack[listAddr].Type == TaggedValueType.Unbound)
                    throw new InstantiationException("list argument to item must be instantiated");
                var list = DataStack[listAddr].Value as IList;
                if (list == null)
                    throw new ArgumentException("List argument to item must be a list.");
                var indexAddr = Deref(argBase + 1);
                var elementAddr = Deref(argBase + 2);
                if (DataStack[indexAddr].Type == TaggedValueType.Unbound)
                {
                    if (DataStack[elementAddr].Type == TaggedValueType.Unbound)
                    {
                        throw new ArgumentException("Enumeration of elements by item not currently supported.");
                    }
                    // Element argument is bound
                    var index = list.IndexOf(DataStack[elementAddr].Value);
                    if (index < 0)
                        return CallStatus.Fail;
                    DataStack[indexAddr].Set(index);
                    SaveVariable(indexAddr);
                    return CallStatus.DeterministicSuccess;
                }
                // Index argument is bound
                if (DataStack[indexAddr].Type != TaggedValueType.Integer)
                    throw new ArgumentException("Index argument to item must be an integer.");
                if (DataStack[elementAddr].Type == TaggedValueType.Unbound)
                {
                    DataStack[elementAddr].SetGeneral(list[DataStack[indexAddr].integer]);
                    SaveVariable(elementAddr);
                }
                else
                {
                    // Both are bound
                    return Equals(DataStack[elementAddr].Value, list[DataStack[indexAddr].integer])?CallStatus.DeterministicSuccess : CallStatus.Fail;
                }
                return CallStatus.DeterministicSuccess;
            });

            Functions.DeclareFunction("item", 2);
            Functions.DeclareFunction("listof", 1);
            Functions.DeclareFunction("setof", 1);
#endregion

#region Meta-operations
            DefinePrimop("call", 2, (argBase, ignore) =>
            {
                var addr1 = Deref(argBase);
                var sym = DataStack[addr1].reference as Symbol;
                if (DataStack[addr1].Type != TaggedValueType.Reference || sym == null)
                    throw new ArgumentException("Predicate name argument to call should be a symbol, but got "+DataStack[addr1].Value);
                DataStack[argBase].reference = Predicate(sym, DataStack[argBase + 1].integer);
                return CallStatus.CallIndirect;
            });
#endregion

#region Global variables
            DefinePrimop("set_global!", 2, (argBase, ignore) =>
            {
                var nameAddr = Deref(argBase);
                var name = DataStack[nameAddr].reference as Symbol;
                if (name == null || DataStack[nameAddr].Type != TaggedValueType.Reference)
                    throw new ArgumentException("Invalid global variable name: "+DataStack[nameAddr].Value);
                var valueAddr = Deref(argBase + 1);
                if (DataStack[valueAddr].Type == TaggedValueType.Unbound)
                    throw new InstantiationException("Value argument to set_global! is uninstantiated");
                var gv = GlobalVariable.Find(name);
                if (gv == null)
                    throw new ArgumentException("Unknown global variable: "+name);
                gv.Value = DataStack[valueAddr];
                return CallStatus.DeterministicSuccess;
            },
            mandatoryInstatiation: true, deterministic: true);

            // Like set_global!, but backtrackable
            DefinePrimop("try_set_global!", 2, (argBase, ignore) =>
            {
                var nameAddr = Deref(argBase);
                var name = DataStack[nameAddr].reference as Symbol;
                if (name == null || DataStack[nameAddr].Type != TaggedValueType.Reference)
                    throw new ArgumentException("Invalid global variable name: " + DataStack[nameAddr].Value);
                var valueAddr = Deref(argBase + 1);
                if (DataStack[valueAddr].Type == TaggedValueType.Unbound)
                    throw new InstantiationException("Value argument to try_set_global! is uninstantiated");
                var gv = GlobalVariable.Find(name);
                if (gv == null)
                    throw new ArgumentException("Unknown global variable: " + name);
                UndoStack[UTop++].Set(UndoTrySetGlobal, gv, ref gv.Value);
                gv.Value = DataStack[valueAddr];
                return CallStatus.DeterministicSuccess;
            },
            mandatoryInstatiation: true);
#endregion
        }
        
        static void UndoTrySetGlobal(ref UndoRecord u)
        {
            ((GlobalVariable) u.objArg).Value = u.TaggedArg;
        }

        /// <summary>
        /// Declares the specified function as a primitive operation that always returns deterministic success.
        /// Do not use this unless you know what you're doing.
        /// </summary>
        public static void DefineActionAsPrimop<T0> (string name, Action<T0> fn) {
            KB.DefinePrimop(name, 1, (argBase, ignore) => {
                T0 arg0 = GetPrimopArg<T0>(0);
                fn(arg0);
                return CallStatus.DeterministicSuccess;
            },
            mandatoryInstatiation: true, deterministic: true);
        }

        /// <summary>
        /// Declares the specified function as a primitive operation that always returns deterministic success.
        /// Do not use this unless you know what you're doing.
        /// </summary>
        public static void DefineActionAsPrimop<T0, T1> (string name, Action<T0, T1> fn) {
            KB.DefinePrimop(name, 2, (argBase, ignore) => {
                T0 arg0 = GetPrimopArg<T0>(0);
                T1 arg1 = GetPrimopArg<T1>(1);
                fn(arg0, arg1);
                return CallStatus.DeterministicSuccess;
            },
            mandatoryInstatiation: true, deterministic: true);
        }

        /// <summary>
        /// Declares the specified function as a primitive operation that always returns deterministic success.
        /// Do not use this unless you know what you're doing.
        /// </summary>
        public static void DefineActionAsPrimop<T0, T1, T2> (string name, Action<T0, T1, T2> fn) {
            KB.DefinePrimop(name, 3, (argBase, ignore) => {
                T0 arg0 = GetPrimopArg<T0>(0);
                T1 arg1 = GetPrimopArg<T1>(1);
                T2 arg2 = GetPrimopArg<T2>(2);
                fn(arg0, arg1, arg2);
                return CallStatus.DeterministicSuccess;
            },
            mandatoryInstatiation: true, deterministic: true);
        }

        /// <summary>
        /// Declares the specified function as a primitive operation that always returns deterministic success.
        /// Do not use this unless you know what you're doing.
        /// </summary>
        public static void DefineActionAsPrimop<T0, T1, T2, T3> (string name, Action<T0, T1, T2, T3> fn) {
            KB.DefinePrimop(name, 4, (argBase, ignore) => {
                T0 arg0 = GetPrimopArg<T0>(0);
                T1 arg1 = GetPrimopArg<T1>(1);
                T2 arg2 = GetPrimopArg<T2>(2);
                T3 arg3 = GetPrimopArg<T3>(3);
                fn(arg0, arg1, arg2, arg3);
                return CallStatus.DeterministicSuccess;
            },
            mandatoryInstatiation: true, deterministic: true);
        }

        static T GetPrimopArg<T> (ushort index) {
            return PrimopArgument<T>.Instance.GetValue(index);
        }

#region Primop argument accessors specialized by type
        private interface IPrimopArgument<T>
        {
            T GetValue(ushort index);
        }

        private sealed class PrimopArgument<T> : IPrimopArgument<T>
        {
            internal static readonly IPrimopArgument<T> Instance = PrimopArgumentImpl.Instance as IPrimopArgument<T> ?? new PrimopArgument<T>();

            // Default implementation, when specialized implementations are not available
            T IPrimopArgument<T>.GetValue(ushort index)
            {
                var val = PrimopArgumentImpl.GetPrimopArgument(index);
                if (val.Type != TaggedValueType.Reference)
                {
                    throw new ArgumentException(
                        $"Primop argument {index} is invalid, expected reference type, got {val.Type}");
                }
                return (T)val.reference;
            }
        }

        private sealed class PrimopArgumentImpl : IPrimopArgument<int>, IPrimopArgument<float>, IPrimopArgument<bool>, IPrimopArgument<object>
        {
            internal static readonly PrimopArgumentImpl Instance = new PrimopArgumentImpl();

            internal static TaggedValue GetPrimopArgument(ushort index)
            {
                var addr = Deref(index);
                if (addr >= DataStack.Length)
                {
                    throw new ArgumentException($"Primop argument {index} is invalid, caused stack overflow");
                }
                return DataStack[addr];
            }

            int IPrimopArgument<int>.GetValue(ushort index)
            {
                return GetPrimopArgument(index).integer;
            }

            float IPrimopArgument<float>.GetValue(ushort index)
            {
                return GetPrimopArgument(index).floatingPoint;
            }

            bool IPrimopArgument<bool>.GetValue(ushort index)
            {
                return GetPrimopArgument(index).boolean;
            }

            object IPrimopArgument<object>.GetValue(ushort index)
            {
                return GetPrimopArgument(index).reference;
            }
        }
#endregion
    }
}