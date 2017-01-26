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
using System.Collections.Generic;
using BotL.Unity;

namespace BotL
{
    // ReSharper disable once InconsistentNaming
    public static class KB
    {
        static KB()
        {
            Primops.DefinePrimops();  // Must come first or calls to primops in the librarty code below get confused.
            Lock("|", 2);
            Lock("not", 1);
            Lock("fail", 0);
            Compiler.Compiler.Compile(Symbol.TruePredicate);
            Lock(Symbol.TruePredicate, 0);
            Compiler.Compiler.Compile("X = X");
            Lock("=", 2);
            Compiler.Compiler.Compile("initialize_variable(_)");
            Lock("initialize_variable", 1);
            Compiler.Compiler.Compile("initialize_accumulator(0.0)");
            Compiler.Compiler.Compile("is_true(true)");
            Lock("is_true", 1);
        }

        // ReSharper disable once UnusedMember.Global
        public static bool IsTrue(string predicateName)
        {
            UnityUtilities.SetUnityGlobals(null, null);
            return Engine.Run(predicateName);
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

        public static void LoadTable(string path)
        {
            var table = Compiler.Compiler.LoadTable(path);
            Predicates[new PredicateIndicator(table.Name, table.Arity)] = table;
        }

        private static void Lock(string name, int arity)
        {
            Lock(Symbol.Intern(name), arity);
        }

        private static void Lock(Symbol name, int arity)
        {
            Predicate(new PredicateIndicator(name, arity)).IsLocked = true;
        }

        private static readonly Dictionary<PredicateIndicator, Predicate> Predicates = new Dictionary<PredicateIndicator, Predicate>();

        internal static Predicate Predicate(PredicateIndicator s)
        {
            Predicate result;
            if (!Predicates.TryGetValue(s, out result))
            {
                result = new Predicate(s.Functor, s.Arity);
                Predicates[s] = result;
            }
            return result;
        }

        internal static Predicate Predicate(Symbol call, int arity)
        {
            return Predicate(new PredicateIndicator(call, arity));
        }

        /// <summary>
        /// A predicate you call as meta(predicate/n, arg1, arg2, ..., argn) and that really means
        /// meta(predicate(arg1, ..., argn)).
        /// </summary>
        public static void DefineMetaPrimop(string name, Compiler.Compiler.PredicateImplementation implementation)
        {
            var s = Symbol.Intern(name);
            for (int arity=2; arity < Compiler.Compiler.MaxSpecialPredicateArity; arity++)
                DefinePrimop(s, arity, 0, implementation);
        }

        public static void DefinePrimop(string name, int arity, byte tempVars, Compiler.Compiler.PredicateImplementation implementation)
        {
            DefinePrimop(Symbol.Intern(name), arity, tempVars, implementation);
        }

        public static void DefinePrimop(string name, int arity, Compiler.Compiler.PredicateImplementation implementation)
        {
            DefinePrimop(Symbol.Intern(name), arity, 0, implementation);
        }

        private static void DefinePrimop(Symbol name, int arity, byte tempVars, Compiler.Compiler.PredicateImplementation implementation)
        {
            Predicates[new PredicateIndicator(name, arity)] = Compiler.Compiler.MakePrimop(name, arity, implementation, tempVars);
        }

        public static void DefineTable(string name, int arity)
        {
            DefineTable(Symbol.Intern(name), arity);
        }

        private static void DefineTable(Symbol name, int arity)
        {
            Predicates[new PredicateIndicator(name, arity)] = Compiler.Compiler.MakeTable(name, arity);
        }

        internal static void DefineTable(PredicateIndicator p)
        {
            Predicates[p] = Compiler.Compiler.MakeTable(p.Functor, p.Arity);
        }

        public static void AddTableRow(string name, int arity, params object[] row)
        {
            AddTableRow(Symbol.Intern(name), arity, row);
        }

        private static void AddTableRow(Symbol name, int arity, params object[] row)
        {
            Predicates[new PredicateIndicator(name, arity)].Table.AddRow(row);
        }
    }
}
