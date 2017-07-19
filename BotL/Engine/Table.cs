#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Table.cs" company="Ian Horswill">
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
using System.IO;
using BotL.Compiler;
using BotL.Parser;
using static BotL.Engine;

namespace BotL
{
    /// <summary>
    /// A predicate implemented as a table of tuples of constants.
    /// Tables may not have rules, although their Predicate objects contain a single fake rule
    /// that parses the arguments and calls into the matching code below.
    /// </summary>
    public class Table
    {
        public Table(Symbol name, int arity)
        {
            Name = name;
            Arity = arity;
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public readonly Symbol Name;
        // ReSharper disable once MemberCanBePrivate.Global
        public readonly int Arity;
        private readonly List<object[]> rows = new List<object[]>();
        public string DefinedInFile { get; set; }

        #region Row matching
        /// <summary>
        /// Return row number of the row following the first matching row starting with startRow,
        /// or 0 if no matching row.
        /// Translation: call this with the row number of the first row you haven't checked.  If it returns 0,
        /// there are no remaining matches.  If it returns non-zero, then there is a match.
        /// If canContinue is true, then you should make a choicepoint to start at the returned row number.
        /// </summary>
        internal ushort MatchTableRows(ushort startRow, ushort frameBase, out bool canContinue)
        {
            var trail = TrailTop;
            for (int row = startRow; row < rows.Count; row++)
            {
                if (MatchTableRow(rows[row], frameBase))
                {
                    var nextRow = (ushort)(row + 1);
                    canContinue = nextRow < rows.Count;
                    return nextRow;
                }
                UndoTo(trail);
            }
            canContinue = false;
            return 0;
        }

        private static bool MatchTableRow(object[] row, ushort frameBase)
        {
            for (int i = 0; i < row.Length; i++)
            {
                var v = row[i];
                var addr = Deref(frameBase + i);
                switch (DataStack[addr].Type)
                {
                    case TaggedValueType.Boolean:
                        if (!(v is bool) || (bool)v != DataStack[addr].boolean)
                            return false;
                        break;

                    case TaggedValueType.Integer:
                        if (!(v is int) || (int)v != DataStack[addr].integer)
                            return false;
                        break;

                    case TaggedValueType.Float:
                        // ReSharper disable once CompareOfFloatsByEqualityOperator
                        if (!(v is float) || (float)v != DataStack[addr].floatingPoint)
                            return false;
                        break;

                    case TaggedValueType.Reference:
                        if (!Equals(v, DataStack[addr].reference))
                            return false;
                        break;

                    case TaggedValueType.Unbound:
                        DataStack[addr].SetGeneral(v);
                        SaveVariable(addr);
                        break;
                }
            }
            return true;
        }
        #endregion

        #region Mutation
        public void AddRow(object[] row)
        {
            if (row.Length != Arity)
                throw new ArgumentException("Adding row of wrong length to table");
            // ReSharper disable once ForCanBeConvertedToForeach
            for (int i = 0; i < rows.Count; i++)
            {
                var existingRow = rows[i];
                for (var column = 0; column < row.Length; column++)
                {
                    if (!Equals(row[column], existingRow[column]))
                        goto mismatch;
                }
                // row and existingRow are identical
                return;
                mismatch:
                ;
            }
            rows.Add(row);
        }

        public void AddRows(List<object[]> newRows)
        {
            rows.AddRange(newRows);
        }
        #endregion

        public static void DefineTablePrimops()
        {
            KB.DefineMetaPrimop("assert_internal!", (argBase, ignore) =>
            {
                var predicate = (Predicate) DataStack[Deref(argBase)].reference;
                if (!AllArgumentsInstantiated(argBase+1, predicate.Arity))
                    throw new InstantiationException("All arguments to assert must be instantiated.");
                if (predicate.Table == null)
                    throw new InvalidOperationException("Attempting to add row to a non-table " + predicate);
                predicate.Table.AddRow(GetRow(argBase + 1, predicate.Arity));
                return CallStatus.DeterministicSuccess;
            },
            mandatoryInstantiation: true, deterministic: true);
            
            KB.DefineMetaPrimop("retract_internal!", (argBase, ignore) =>
            {
                var predicate = (Predicate)DataStack[Deref(argBase)].reference;
                if (!AllArgumentsInstantiated(argBase + 1, predicate.Arity))
                    throw new InstantiationException("All arguments to retract must be instantiated.");
                var table = predicate.Table;
                if (table == null)
                    throw new InvalidOperationException("Attempting to add row to a non-table " + predicate);
                var rowNum = table.FindRow(argBase + 1);
                if (rowNum < 0)
                    return CallStatus.Fail;
                predicate.Table.rows.RemoveAt(rowNum);
                return CallStatus.DeterministicSuccess;
            },
            mandatoryInstantiation: true, semiDeterministic: true);

            KB.DefineMetaPrimop("update_internal!", (argBase, ignore) =>
            {
                var predicate = (Predicate)DataStack[Deref(argBase)].reference;
                if (!AllArgumentsInstantiated(argBase + 1, predicate.Arity))
                    throw new InstantiationException("All arguments to update must be instantiated.");
                var table = predicate.Table;
                if (table == null)
                    throw new InvalidOperationException("Attempting to add row to a non-table " + predicate);
                var rowNum = table.FindFunctionRow(argBase + 1);
                if (rowNum < 0)
                    return CallStatus.Fail;
                predicate.Table.rows[rowNum][predicate.Arity - 1] = DataStack[Deref(argBase + predicate.Arity)].Value;
                return CallStatus.DeterministicSuccess;
            },
            mandatoryInstantiation: true, semiDeterministic: true);
            
            KB.DefineMetaPrimop("increment_internal!", (argBase, ignore) =>
            {
                var predicate = (Predicate)DataStack[Deref(argBase)].reference;
                if (!AllArgumentsInstantiated(argBase + 1, predicate.Arity))
                    throw new InstantiationException("All arguments to update must be instantiated.");
                var table = predicate.Table;
                if (table == null)
                    throw new InvalidOperationException("Attempting to add row to a non-table " + predicate);
                var rowNum = table.FindFunctionRow(argBase + 1);
                if (rowNum < 0)
                    return CallStatus.Fail;
                predicate.Table.rows[rowNum][predicate.Arity - 1] =
                    Convert.ToSingle(predicate.Table.rows[rowNum][predicate.Arity - 1]) + DataStack[Deref(argBase + predicate.Arity)].AsFloat;
                return CallStatus.DeterministicSuccess;
            },
            mandatoryInstantiation: true, semiDeterministic: true);

            KB.DefineMetaPrimop("retractall_internal!", (argBase, ignore) =>
            {
                var predicate = (Predicate)DataStack[Deref(argBase)].reference;
                var table = predicate.Table;
                if (table == null)
                    throw new InvalidOperationException("Attempting to add row to a non-table " + predicate);
                var rowNum = 0;
                while ((rowNum = table.FindRowWithWildcards(argBase + 1, rowNum))>= 0)
                    predicate.Table.rows.RemoveAt(rowNum);
                return CallStatus.DeterministicSuccess;
            },
            deterministic: true);
        }

        #region Helper functions for primops
        private static object[] GetRow(int baseAddress, int length)
        {
            var row = new object[length];
            for (int i = 0; i < row.Length; i++)
            {
                row[i] = DataStack[Deref(baseAddress + i)].Value;
            }
            return row;
        }

        private static bool AllArgumentsInstantiated(int baseAddress, int length)
        {
            for (int a = baseAddress; a < baseAddress + length; a++)
                if (DataStack[Deref(a)].Type == TaggedValueType.Unbound)
                    return false;
            return true;
        }

        private int FindRow(int baseAddress)
        {
            for (var rowNum = 0; rowNum < rows.Count; rowNum++)
            {
                var row = rows[rowNum];
                for (int i = 0; i < row.Length; i++)
                {
                    if (!DataStack[Deref(baseAddress + i)].EqualGeneral(row[i]))
                        goto mismatch;
                }
                return rowNum;
                mismatch:
                ;
            }
            return -1;
        }

        private int FindFunctionRow(int baseAddress)
        {
            for (var rowNum = 0; rowNum < rows.Count; rowNum++)
            {
                var row = rows[rowNum];
                for (int i = 0; i < row.Length-1; i++)
                {
                    if (!DataStack[Deref(baseAddress + i)].EqualGeneral(row[i]))
                        goto mismatch;
                }
                return rowNum;
                mismatch:
                ;
            }
            return -1;
        }

        private int FindRowWithWildcards(int baseAddress, int startRow)
        {
            for (var rowNum = startRow; rowNum < rows.Count; rowNum++)
            {
                var row = rows[rowNum];
                for (int i = 0; i < row.Length; i++)
                {
                    var addr = Deref(baseAddress + i);
                    if (DataStack[addr].Type == TaggedValueType.Unbound)
                        continue;
                    if (!DataStack[addr].EqualGeneral(row[i]))
                        goto mismatch;
                }
                return rowNum;
                mismatch:
                ;
            }
            return -1;
        }
        #endregion

        public static void DefineTableMacros()
        {
            Macros.DeclareMacro("assert!", 1, arg => Expand(Symbol.Intern("assert_internal!"), arg));
            Macros.DeclareMacro("update!", 1, arg => Expand(Symbol.Intern("update_internal!"), arg));
            Macros.DeclareMacro("increment!", 1, arg => Expand(Symbol.Intern("increment_internal!"), arg));
            Macros.DeclareMacro("retract!", 1, arg => Expand(Symbol.Intern("retract_internal!"), arg));
            Macros.DeclareMacro("retractall!", 1, arg => Expand(Symbol.Intern("retractall_internal!"), arg));
        }

        private static object Expand(Symbol functor, object arg)
        {
            if (Variable.IsVariableExpression(arg))
                return ELNode.ExpandUpdate(functor, arg);
            var c = arg as Call;
            if (c==null)
                throw new SyntaxError("Invalid assertion in assert!, update!, etc.", arg);
            if (c.Functor == Symbol.Slash || c.Functor == Symbol.Colon
                || c.Functor.Name == ">>" || c.Functor == ELNode.WriteToEnd)
                return ELNode.ExpandUpdate(functor, arg);
            if (c.IsFunctor(Symbol.Implication, 2))
                throw new ArgumentException("Assert!/2 is used only for updating tables, not for adding rules to rule predicates.");
            var arglist = new object[c.Arguments.Length + 1];
            arglist[0] = KB.Predicate(new PredicateIndicator(c.Functor, c.Arity));
            for (var i = 0; i < c.Arguments.Length; i++)
                arglist[i + 1] = c.Arguments[i];
            return new Call(functor, arglist);
        }

        public override string ToString()
        {
            return $"Table<{Name}/{Arity}>";
        }

        public void Listing(TextWriter stream)
        {
            if (rows.Count == 0)
                stream.WriteLine("{0} is an empty table");
            else
                foreach (var row in rows)
                {
                    foreach (var item in row)
                    {
                        stream.Write(ExpressionParser.WriteExpressionToString(item));
                        stream.Write(" ");
                    }
                    stream.WriteLine();
                }
        }
    }
}
