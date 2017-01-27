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

namespace BotL
{
    public static class Primops
    {
        internal static void DefinePrimops()
        {
            Table.DefineTablePrimops();

            #region Equality testing
            // Nonunifiability test
            KB.DefinePrimop("\\=", 2, (argBase, ignore) =>
            {
                var addr1 = Engine.Deref(argBase);
                var addr2 = Engine.Deref(argBase + 1);
                if (Engine.DataStack[addr1].Type == TaggedValueType.Unbound || Engine.DataStack[addr2].Type == TaggedValueType.Unbound)
                    return CallStatus.Fail;
                if (Engine.DataStack[addr1].Type != Engine.DataStack[addr2].Type)
                    return CallStatus.Fail;
                switch (Engine.DataStack[addr1].Type)
                {
                    case TaggedValueType.Boolean:
                        return (Engine.DataStack[addr1].boolean != Engine.DataStack[addr2].boolean)?CallStatus.DeterministicSuccess : CallStatus.Fail;

                    case TaggedValueType.Integer:
                        return (Engine.DataStack[addr1].integer != Engine.DataStack[addr2].integer) ? CallStatus.DeterministicSuccess : CallStatus.Fail;

                    case TaggedValueType.Float:
                        // ReSharper disable once CompareOfFloatsByEqualityOperator
                        return (Engine.DataStack[addr1].floatingPoint != Engine.DataStack[addr2].floatingPoint) ? CallStatus.DeterministicSuccess : CallStatus.Fail;

                    default:
                        return (!Equals(Engine.DataStack[addr1].reference, Engine.DataStack[addr2].reference)) ? CallStatus.DeterministicSuccess : CallStatus.Fail;
                }
            });
            #endregion

            #region Numerical operations
            // Numerical comparisons
            KB.DefinePrimop(">", 2, (argBase, ignore) => (Engine.DataStack[Engine.Deref(argBase)].AsFloat > Engine.DataStack[Engine.Deref(argBase + 1)].AsFloat) ? CallStatus.DeterministicSuccess : CallStatus.Fail);
            KB.DefinePrimop(">=", 2, (argBase, ignore) => (Engine.DataStack[Engine.Deref(argBase)].AsFloat >= Engine.DataStack[Engine.Deref(argBase + 1)].AsFloat) ? CallStatus.DeterministicSuccess : CallStatus.Fail);
            KB.DefinePrimop("<", 2, (argBase, ignore) => (Engine.DataStack[Engine.Deref(argBase)].AsFloat < Engine.DataStack[Engine.Deref(argBase + 1)].AsFloat) ? CallStatus.DeterministicSuccess : CallStatus.Fail);
            KB.DefinePrimop("=<", 2, (argBase, ignore) => (Engine.DataStack[Engine.Deref(argBase)].AsFloat <= Engine.DataStack[Engine.Deref(argBase + 1)].AsFloat) ? CallStatus.DeterministicSuccess : CallStatus.Fail);
            #endregion

            #region Utility primops for aggregation (min/max, summation)
            KB.DefinePrimop("aggregate_sum", 2, (argBase, ignore) =>
            {
                var numAddr = Engine.Deref(argBase);
                var resultAddr = Engine.Deref(argBase + 1);
                Engine.DataStack[resultAddr].floatingPoint += Engine.DataStack[numAddr].AsFloat;
                return CallStatus.Fail;
            });

            KB.DefinePrimop("aggregate_min", 2, (argBase, ignore) =>
            {
                var numAddr = Engine.Deref(argBase);
                var resultAddr = Engine.Deref(argBase + 1);
                var newValue = Engine.DataStack[numAddr].AsFloat;
                if (Engine.DataStack[resultAddr].Type == TaggedValueType.Unbound
                     || newValue < Engine.DataStack[resultAddr].floatingPoint)
                    Engine.DataStack[resultAddr].Set(newValue);
                return CallStatus.Fail;
            });

            KB.DefinePrimop("aggregate_max", 2, (argBase, ignore) =>
            {
                var numAddr = Engine.Deref(argBase);
                var resultAddr = Engine.Deref(argBase + 1);
                var newValue = Engine.DataStack[numAddr].AsFloat;
                if (Engine.DataStack[resultAddr].Type == TaggedValueType.Unbound
                     || newValue > Engine.DataStack[resultAddr].floatingPoint)
                    Engine.DataStack[resultAddr].Set(newValue);
                return CallStatus.Fail;
            });

            KB.DefinePrimop("aggregate_argmin", 4, (argBase, ignore) =>
            {
                var numAddr = Engine.Deref(argBase);
                var numResultAddr = Engine.Deref(argBase + 1);
                var newValue = Engine.DataStack[numAddr].AsFloat;
                if (Engine.DataStack[numResultAddr].Type == TaggedValueType.Unbound
                    || newValue < Engine.DataStack[numResultAddr].floatingPoint)
                {
                    Engine.DataStack[numResultAddr].Set(newValue);
                    Engine.DataStack[Engine.Deref(argBase + 3)] = Engine.DataStack[Engine.Deref(argBase + 2)];
                }
                return CallStatus.Fail;
            });

            KB.DefinePrimop("aggregate_argmax", 4, (argBase, ignore) =>
            {
                var numAddr = Engine.Deref(argBase);
                var numResultAddr = Engine.Deref(argBase + 1);
                var newValue = Engine.DataStack[numAddr].AsFloat;
                if (Engine.DataStack[numResultAddr].Type == TaggedValueType.Unbound
                    || newValue > Engine.DataStack[numResultAddr].floatingPoint)
                {
                    Engine.DataStack[numResultAddr].Set(newValue);
                    Engine.DataStack[Engine.Deref(argBase + 3)] = Engine.DataStack[Engine.Deref(argBase + 2)];
                }
                return CallStatus.Fail;
            });
            #endregion

            #region Type testing
            // Binding tests
            KB.DefinePrimop("var", 1, (argBase, ignore) => (Engine.DataStack[Engine.Deref(argBase)].Type == TaggedValueType.Unbound) ? CallStatus.DeterministicSuccess : CallStatus.Fail);
            KB.DefinePrimop("nonvar", 1, (argBase, ignore) => (Engine.DataStack[Engine.Deref(argBase)].Type != TaggedValueType.Unbound) ? CallStatus.DeterministicSuccess : CallStatus.Fail);

            // Type tests
            KB.DefinePrimop("integer", 1, (argBase, ignore) => (Engine.DataStack[Engine.Deref(argBase)].Type == TaggedValueType.Integer) ? CallStatus.DeterministicSuccess : CallStatus.Fail);
            KB.DefinePrimop("float", 1, (argBase,ignore) => (Engine.DataStack[Engine.Deref(argBase)].Type == TaggedValueType.Float) ? CallStatus.DeterministicSuccess : CallStatus.Fail);
            KB.DefinePrimop("number", 1, (argBase, ignore) =>
            {
                var addr = Engine.Deref(argBase);
                return (Engine.DataStack[addr].Type == TaggedValueType.Float ||
                       Engine.DataStack[addr].Type == TaggedValueType.Integer) ? CallStatus.DeterministicSuccess : CallStatus.Fail;
            });
            KB.DefinePrimop("symbol", 1, (argBase, ignore) =>
            {
                var addr = Engine.Deref(argBase);
                return (Engine.DataStack[addr].Type == TaggedValueType.Reference &&
                       Engine.DataStack[addr].reference != null &&
                       Engine.DataStack[addr].reference is Symbol) ? CallStatus.DeterministicSuccess : CallStatus.Fail;
            });
            KB.DefinePrimop("string", 1, (argBase, ignore) =>
            {
                var addr = Engine.Deref(argBase);
                return (Engine.DataStack[addr].Type == TaggedValueType.Reference &&
                       Engine.DataStack[addr].reference != null &&
                       Engine.DataStack[addr].reference is string) ? CallStatus.DeterministicSuccess : CallStatus.Fail;
            });

            KB.DefinePrimop("missing", 1, (argBase, ignore) =>
            {
                var addr = Engine.Deref(argBase);
                return (Engine.DataStack[addr].Type == TaggedValueType.Reference &&
                        Engine.DataStack[addr].reference == Structs.PaddingValue)
                    ? CallStatus.DeterministicSuccess
                    : CallStatus.Fail;
            });
            #endregion

            #region IO
            KB.DefinePrimop("write", 1, (argBase, ignore) =>
            {
                Repl.StandardOutput.Write(Engine.DataStack[Engine.Deref(argBase)]);
                return CallStatus.DeterministicSuccess;
            });

            KB.DefinePrimop("writenl", 1, (argBase, ignore) =>
            {
                Repl.StandardOutput.WriteLine(Engine.DataStack[Engine.Deref(argBase)]);
                return CallStatus.DeterministicSuccess;
            });
            #endregion

            #region Environment-related stuff
            KB.DefinePrimop("load", 1, (argBase, ignore) =>
            {
                var addr = Engine.Deref(argBase);
                var path = Engine.DataStack[addr].reference as string;
                if (Engine.DataStack[addr].Type == TaggedValueType.Reference && path != null)
                {
                    Compiler.Compiler.CompileFile(path);
                }
                else
                    throw new ArgumentException("Argument to load must be a string");
                return CallStatus.DeterministicSuccess;
            });

            KB.DefinePrimop("load_table", 1, (argBase, ignore) =>
            {
                var addr = Engine.Deref(argBase);
                var path = Engine.DataStack[addr].reference as string;
                if (Engine.DataStack[addr].Type == TaggedValueType.Reference && path != null)
                {
                    KB.LoadTable(path);
                }
                else
                    throw new ArgumentException("Argument to load_table must be a string");
                return CallStatus.DeterministicSuccess;
            });
            #endregion

            #region C# interop
            KB.DefinePrimop("in", 2, 1, (argBase, restartCount) =>
            {
                var memberAddr = Engine.Deref(argBase);
                var collectionAddr = Engine.Deref(argBase + 1);
                var enumeratorAddr = argBase + 2;

                if (Engine.DataStack[collectionAddr].Type != TaggedValueType.Reference)
                {
                    throw new ArgumentException("Invalid collecction argument to in/2.");
                }

                var collection = Engine.DataStack[collectionAddr].Value;
                if (Engine.DataStack[memberAddr].Type == TaggedValueType.Unbound)
                {
                    // We're enumerating
                    var ilist = collection as IList;
                    if (ilist != null)
                    {
                        if (ilist.Count==0)
                            return CallStatus.Fail;
                        // Enumerating an IList
                        Engine.DataStack[memberAddr].SetGeneral(ilist[restartCount]);
                        Engine.SaveUndo(memberAddr);
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
                            Engine.DataStack[enumeratorAddr].reference = ienumerable.GetEnumerator();
                        }
                        var enumerator = (IEnumerator) Engine.DataStack[enumeratorAddr].reference;
                        if (enumerator.MoveNext())
                        {
                            Engine.DataStack[memberAddr].SetGeneral(enumerator.Current);
                            Engine.SaveUndo(memberAddr);
                            return CallStatus.NonDeterministicSuccess;
                        }
                        else return CallStatus.Fail;
                    }
                }
                else
                {
                    // We're testing membership
                    var member = Engine.DataStack[memberAddr].Value;
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
            });

            KB.DefinePrimop("adjoin", 2, (argBase, ignore) =>
            {
                var addr = Engine.Deref(argBase);
                if (Engine.DataStack[addr].Type == TaggedValueType.Unbound)
                    throw new InstantiationException("collection argument to adjoin must be instantiated");
                var collection = Engine.DataStack[addr].Value;
                addr = Engine.Deref(argBase + 1);
                if (Engine.DataStack[addr].Type == TaggedValueType.Unbound)
                    throw new InstantiationException("element argument to adjoin must be instantiated");
                var element = Engine.DataStack[addr].Value;

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
            });

            KB.DefinePrimop("item", 3, (argBase, restartCount) =>
            {
                var listAddr = Engine.Deref(argBase);
                if (Engine.DataStack[listAddr].Type == TaggedValueType.Unbound)
                    throw new InstantiationException("list argument to item must be instantiated");
                var list = Engine.DataStack[listAddr].Value as IList;
                if (list == null)
                    throw new ArgumentException("List argument to item must be a list.");
                var indexAddr = Engine.Deref(argBase + 1);
                var elementAddr = Engine.Deref(argBase + 2);
                if (Engine.DataStack[indexAddr].Type == TaggedValueType.Unbound)
                {
                    if (Engine.DataStack[elementAddr].Type == TaggedValueType.Unbound)
                    {
                        throw new ArgumentException("Enumeration of elements by item not currently supported.");
                    }
                    // Element argument is bound
                    var index = list.IndexOf(Engine.DataStack[elementAddr].Value);
                    if (index < 0)
                        return CallStatus.Fail;
                    Engine.DataStack[indexAddr].Set(index);
                    Engine.SaveUndo(indexAddr);
                    return CallStatus.DeterministicSuccess;
                }
                // Index argument is bound
                if (Engine.DataStack[indexAddr].Type != TaggedValueType.Integer)
                    throw new ArgumentException("Index argument to item must be an integer.");
                if (Engine.DataStack[elementAddr].Type == TaggedValueType.Unbound)
                {
                    Engine.DataStack[elementAddr].SetGeneral(list[Engine.DataStack[indexAddr].integer]);
                    Engine.SaveUndo(elementAddr);
                }
                else
                {
                    // Both are bound
                    return Equals(Engine.DataStack[elementAddr].Value, list[Engine.DataStack[indexAddr].integer])?CallStatus.DeterministicSuccess : CallStatus.Fail;
                }
                return CallStatus.DeterministicSuccess;
            });

            Functions.DeclareFunction("item", 2);
            Functions.DeclareFunction("listof", 1);
            Functions.DeclareFunction("setof", 1);
            #endregion

            #region Meta-operations
            KB.DefinePrimop("call", 2, (argBase, ignore) =>
            {
                var addr1 = Engine.Deref(argBase);
                var sym = Engine.DataStack[addr1].reference as Symbol;
                if (Engine.DataStack[addr1].Type != TaggedValueType.Reference || sym == null)
                    throw new ArgumentException("Predicate name argument to call should be a symbol, but got "+Engine.DataStack[addr1].Value);
                Engine.DataStack[argBase].reference = KB.Predicate(sym, Engine.DataStack[argBase + 1].integer);
                return CallStatus.CallIndirect;
            });
            #endregion

            #region Global variables
            KB.DefinePrimop("set_global", 2, (argBase, ignore) =>
            {
                var nameAddr = Engine.Deref(argBase);
                var name = Engine.DataStack[nameAddr].reference as Symbol;
                if (name == null || Engine.DataStack[nameAddr].Type != TaggedValueType.Reference)
                    throw new ArgumentException("Invalid global variable name: "+Engine.DataStack[nameAddr].Value);
                var valueAddr = Engine.Deref(argBase + 1);
                if (Engine.DataStack[valueAddr].Type == TaggedValueType.Unbound)
                    throw new InstantiationException("Value argument to set_global is uninstantiated");
                var gv = GlobalVariable.Find(name);
                if (gv == null)
                    throw new ArgumentException("Unknown global variable: "+name);
                gv.Value = Engine.DataStack[valueAddr];
                return CallStatus.DeterministicSuccess;
            });
            #endregion
        }
    }
}