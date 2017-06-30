#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="KB.cs" company="Ian Horswill">
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
    /// <summary>
    /// The BotL knowledge base, i.e. all the predicates and their associated rules, tables, and primops.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class KB
    {
        internal static readonly GlobalVariable TopLevelPredicate = GlobalVariable.DefineGlobal("%topLevelPredicate", null);
        internal static readonly GlobalVariable TopLevelArgV = GlobalVariable.DefineGlobal("%topLevelArgv", null);

        static KB()
        {
            Primops.DefinePrimops(); // Must come first or calls to primops in the librarty code below get confused.
            Lock("|", 2);
            Lock("not", 1);
            Lock("fail", 0);
            Compiler.Compiler.Compile(Symbol.TruePredicate);
            Lock(Symbol.TruePredicate, 0);
            Compiler.Compiler.Compile("X = X");
            Lock("=", 2);
            Compiler.Compiler.Compile("apply(P, L) <-- C=length(L), apply_internal(P, C, L)");
            Compiler.Compiler.Compile("apply_internal(P, 0, _) <-- !, call(P)");
            Compiler.Compiler.Compile("apply_internal(P, 1, L) <-- !, call(P, L[0])");
            Compiler.Compiler.Compile("apply_internal(P, 2, L) <-- !, call(P, L[0], L[1])");
            Compiler.Compiler.Compile("apply_internal(P, 3, L) <-- !, call(P, L[0], L[1], L[2])");
            Compiler.Compiler.Compile("apply_internal(P, 4, L) <-- !, call(P, L[0], L[1], L[2], L[3])");
            Compiler.Compiler.Compile("apply_internal(P, 5, L) <-- !, call(P, L[0], L[1], L[2], L[3], L[4])");
            Compiler.Compiler.Compile("apply_internal(P, 6, L) <-- !, call(P, L[0], L[1], L[2], L[3], L[4], L[5])");
            Compiler.Compiler.Compile("apply_internal(P, 7, L) <-- !, call(P, L[0], L[1], L[2], L[3], L[4], L[5], L[6])");
            Compiler.Compiler.Compile("apply_internal(P, 8, L) <-- !, call(P, L[0], L[1], L[2], L[3], L[4], L[5], L[6], L[7])");
            Compiler.Compiler.Compile("apply_internal(_, _, _) <-- '%call_failed'(apply)");
            Lock("apply",2);
            Lock("apply_internal", 3);

            Compiler.Compiler.Compile("'%apply_top_level' <-- apply($'%topLevelPredicate', $'%topLevelArgv')");
            Lock("%apply_top_level", 0);
        }

        /// <summary>
        /// Call the predicate with specified name, without arguments.
        /// NOT REENTRANT: do not call this from methods that were themselves
        /// called from BotL code.
        /// </summary>
        /// <param name="predicateName">Name of the predicate</param>
        /// <returns>True if the predicate succeeded.</returns>
        // ReSharper disable once UnusedMember.Global
        public static bool IsTrue(string predicateName)
        {
            UnityUtilities.SetUnityGlobals(null, null);
            return Engine.Run(predicateName);
        }

        /// <summary>
        /// Call the predicate with specified name, without arguments.
        /// Intended for imperatives - throws exception if predicate doesn't succeed.
        /// NOT REENTRANT: do not call this from methods that were themselves
        /// called from BotL code.
        /// </summary>
        /// <param name="predicateName">Name of the predicate</param>
        // ReSharper disable once UnusedMember.Global
        public static void Run(string predicateName)
        {
            if (!IsTrue(predicateName))
                throw new CallFailedException(Symbol.Intern(predicateName));
        }

        /// <summary>
        /// Compile a single statement (rule or fact) and add it to the KB.
        /// </summary>
        /// <param name="statement"></param>
        // ReSharper disable once UnusedMember.Global
        public static void Compile(string statement)
        {
            Compiler.Compiler.Compile(statement);
        }

        /// <summary>
        /// Compile and load code from a .bot file into KB.
        /// </summary>
        /// <param name="path">Path to file</param>
        // ReSharper disable once UnusedMember.Global
        public static void Load(string path)
        {
            Compiler.Compiler.CompileFile(path);
        }

        /// <summary>
        /// Load a table from a CSV file.
        /// </summary>
        /// <param name="path"></param>
        public static void LoadTable(string path)
        {
            var table = Compiler.Compiler.LoadTable(path);
            PredicateTable[new PredicateIndicator(table.Name, table.Arity)] = table;
        }

        /// <summary>
        /// Disallow the addition of further rules to a predicate
        /// </summary>
        /// <param name="name">Name of the predicate</param>
        /// <param name="arity">Arity of the predicate</param>
        private static void Lock(string name, int arity)
        {
            Lock(Symbol.Intern(name), arity);
        }

        /// <summary>
        /// Disallow the addition of further rules to a predicate
        /// </summary>
        /// <param name="name">Name of the predicate</param>
        /// <param name="arity">Arity of the predicate</param>
        private static void Lock(Symbol name, int arity)
        {
            Predicate(new PredicateIndicator(name, arity)).IsLocked = true;
        }

        private static readonly Dictionary<PredicateIndicator, Predicate> PredicateTable = new Dictionary<PredicateIndicator, Predicate>();

        internal static IEnumerable<Predicate> AllPredicates
        {
            get
            {
                foreach (var pair in PredicateTable)
                {
                    yield return pair.Value;
                }
            }
        }

        internal static IEnumerable<Predicate> AllRulePredicates
        {
            get
            {
                foreach (var pair in PredicateTable)
                {
                    if (pair.Value.IsRulePredicate)
                        yield return pair.Value;
                }
            }
        }

        internal static Predicate Predicate(PredicateIndicator s)
        {
            Predicate result;
            if (!PredicateTable.TryGetValue(s, out result))
            {
                result = new Predicate(s.Functor, s.Arity);
                PredicateTable[s] = result;
            }
            return result;
        }

        internal static Predicate Predicate(Symbol call, int arity)
        {
            return Predicate(new PredicateIndicator(call, arity));
        }

        #region Primop declarations
        /// <summary>
        /// A predicate you call as meta(predicate/n, arg1, arg2, ..., argn) and that really means
        /// meta(predicate(arg1, ..., argn)).
        /// </summary>
        public static void DefineMetaPrimop(string name, Compiler.Compiler.PredicateImplementation implementation,
            bool mandatoryInstantiation = false,
            bool deterministic = false,
            bool semiDeterministic = false)
        {
            for (int arity=2; arity < Compiler.Compiler.MaxSpecialPredicateArity; arity++)
                DefinePrimop(name, arity, implementation,
                    mandatoryInstatiation: mandatoryInstantiation,
                    deterministic: deterministic,
                    semiDeterministic: semiDeterministic);
        }

        /// <summary>
        /// Define a new primop.  Don't use this unless you know what you're doing.
        /// </summary>
        /// <param name="name">Name of the primop</param>
        /// <param name="arity">Arity</param>
        /// <param name="implementation">Delegate to implement the primop</param>
        /// <param name="tempVars">Number of temporary variables to preserve in the primop's stack frame.</param>
        /// <param name="mandatoryInstatiation">Whether the primop requires its arguments to be instantiated.  If so, the compiler will issue a warning when it can prove one of the arguments will be uninstantiated at the time of a call to this primop.</param>
        /// <param name="deterministic">True if this primop will always either succeed exactly once or throw an exception</param>
        /// <param name="semiDeterministic">True if this primop will never succeed more than once.</param>
        public static void DefinePrimop(string name, int arity,
            Compiler.Compiler.PredicateImplementation implementation,
            byte tempVars = 0,
            bool mandatoryInstatiation = false, 
            bool deterministic=false,
            bool semiDeterministic = false)
        {
            var nameSym = Symbol.Intern(name);
            var primop = Compiler.Compiler.MakePrimop(nameSym, arity, implementation, tempVars);
            PredicateTable[new PredicateIndicator(nameSym, arity)] = primop;
            primop.MandatoryInstantiation = mandatoryInstatiation;
            primop.Deterministic = deterministic;
            primop.SemiDeterministic = semiDeterministic;
        }
        #endregion

        #region Table declarations
        /// <summary>
        /// Create an empty table.
        /// </summary>
        public static void DefineTable(string name, int arity)
        {
            DefineTable(Symbol.Intern(name), arity);
        }

        /// <summary>
        /// Create an empty table.
        /// </summary>
        private static void DefineTable(Symbol name, int arity)
        {
            var p = new PredicateIndicator(name, arity);
            if (PredicateTable.ContainsKey(p))
                throw new InvalidOperationException($"Predicate {p} is already defined");
            PredicateTable[p] = Compiler.Compiler.MakeTable(name, arity);
        }

        /// <summary>
        /// Create an empty table.
        /// </summary>
        internal static void DefineTable(PredicateIndicator p)
        {
            if (PredicateTable.ContainsKey(p))
                throw new InvalidOperationException($"Predicate {p} is already defined");
            PredicateTable[p] = Compiler.Compiler.MakeTable(p.Functor, p.Arity);
        }

        /// <summary>
        /// Add a row to an existing table.
        /// </summary>
        public static void AddTableRow(string name, int arity, params object[] row)
        {
            AddTableRow(Symbol.Intern(name), arity, row);
        }

        /// <summary>
        /// Add a row to an existing table.
        /// </summary>
        private static void AddTableRow(Symbol name, int arity, params object[] row)
        {
            PredicateTable[new PredicateIndicator(name, arity)].Table.AddRow(row);
        }
        #endregion
    }
}
