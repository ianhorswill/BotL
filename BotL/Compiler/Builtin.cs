#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Builtin.cs" company="Ian Horswill">
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

namespace BotL.Compiler
{
    /// <summary>
    /// Builtins are deterministic predicates that are in-lined into the bytecode of a rule
    /// rather than being implemented by an out-of-line call to a primop or a rule.
    /// </summary>
    public enum Builtin
    {
        /// <summary>
        /// Format: Fail
        /// Always fails
        /// </summary>
        Fail,
        /// <summary>
        /// Format: Var Index
        /// Fails if the var at the specified index is bound
        /// </summary>
        Var,
        /// <summary>
        /// Format: Novar Index
        /// Fails if the var at the specified index is unbound
        /// </summary>
        NonVar,
        /// <summary>
        /// Forcibly unbind a variable
        /// </summary>
        UnsafeInitialize,
        /// <summary>
        /// Forcibly set a variable to 0
        /// </summary>
        UnsafeInitializeZero,
        /// <summary>
        /// Forcibly set a variable to the current value of another (does not alias)
        /// </summary>
        UnsafeSet,
        /// <summary>
        /// Updates first argument if it is is unbound or larger than second
        /// </summary>
        MaximizeUpdate,
        /// <summary>
        /// Updates first argument if it is is unbound or less than second
        /// </summary>
        MinimizeUpdate,
        /// <summary>
        /// Updates first argument if it is unbound or larger than second, then fails
        /// </summary>
        MaximizeUpdateAndRepeat,
        /// <summary>
        /// Updates first argument if it is unbound or less than second, then fails
        /// </summary>
        MinimizeUpdateAndRepeat,
        /// <summary>
        /// Add second argument to first and fail.
        /// </summary>
        SumUpdateAndRepeat,
        /// <summary>
        /// Test that a functional expression evaluates to a non-false value
        /// Used for treating method calls as predicates
        /// </summary>
        TestNotFalse,

        // Numeric comparisons
        LessThan,
        LessEq,
        GreaterThan,
        GreaterEq,

        // Type tests

        IntegerTest,
        FloatTest,
        NumberTest,
        StringTest,
        SymbolTest,
        MissingTest
    }

    internal static class BuiltinTable
    {
        static BuiltinTable()
        {
            DefineBuiltin("var", 1, Builtin.Var);
            DefineBuiltin("nonvar", 1, Builtin.NonVar);
            DefineBuiltin("%init", 1, Builtin.UnsafeInitialize);
            DefineBuiltin("%init_zero", 1, Builtin.UnsafeInitializeZero);
            DefineBuiltin("%unsafe_set", 2, Builtin.UnsafeSet);
            DefineBuiltin("%maximize_update", 2, Builtin.MaximizeUpdate);
            DefineBuiltin("%minimize_update", 2, Builtin.MinimizeUpdate);
            DefineBuiltin("%maximize_update_and_repeat", 2, Builtin.MaximizeUpdateAndRepeat);
            DefineBuiltin("%minimize_update_and_repeat", 2, Builtin.MinimizeUpdateAndRepeat);
            DefineBuiltin("%sum_update_and_repeat", 2, Builtin.SumUpdateAndRepeat);
            DefineBuiltin("<", 2, Builtin.LessThan);
            DefineBuiltin("=<", 2, Builtin.LessEq);
            DefineBuiltin(">", 2, Builtin.GreaterThan);
            DefineBuiltin(">=", 2, Builtin.GreaterEq);
            DefineBuiltin("integer", 1, Builtin.IntegerTest);
            DefineBuiltin("float", 1, Builtin.FloatTest);
            DefineBuiltin("number", 1, Builtin.NumberTest);
            DefineBuiltin("string", 1, Builtin.StringTest);
            DefineBuiltin("symbol", 1, Builtin.SymbolTest);
            DefineBuiltin("missing", 1, Builtin.MissingTest);
            DefineBuiltin("%not_false", 1, Builtin.TestNotFalse);
        }

        static readonly Dictionary<PredicateIndicator, Builtin> Table = new Dictionary<PredicateIndicator, Builtin>();

        static void DefineBuiltin(string name, int arity , Builtin code)
        {
            Table[new PredicateIndicator(name, arity)] = code;
        }

        public static Builtin? BuiltinOpcode(Call c)
        {
            if (Table.TryGetValue(new PredicateIndicator(c), out Builtin b))
                return b;
            return null;
        }
    }
}