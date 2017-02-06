#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Macros.cs" company="Ian Horswill">
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
    static class Macros
    {
        public static void DeclareMacros()
        {
            Table.DefineTableMacros();
            ELNode.DefineElMacros();

            DeclareMacro("not", 1, p => Or(And(p, Symbol.Cut, Symbol.Fail), Symbol.TruePredicate));
            DeclareMacro("->", 2, (test, consequent) => And(test, Symbol.Cut, consequent));
            // The or fail here is to place the cut in its own context to prevent it from cutting
            // the parent clause.
            DeclareMacro("once", 1, exp => Or(Symbol.Fail, And(exp, Symbol.Cut)));
            DeclareMacro("ignore", 1, exp => Or(And(exp, Symbol.Cut), Symbol.TruePredicate));
            DeclareMacro("forall", 2, (cond, action) => Not(And(cond, Not(action))));
            DeclareMacro("minimum", 3,
                (score, generator, result) => And(new Call("initialize_variable", result),
                                                  Or(And(generator,
                                                         new Call("aggregate_min", score, result),
                                                         Symbol.Fail),
                                                     new Call("nonvar", result))));
            DeclareMacro("maximum", 3,
                (score, generator, result) => And(new Call("initialize_variable", result),
                                                  Or(And(generator,
                                                         new Call("aggregate_max", score, result),
                                                         Symbol.Fail),
                                                     new Call("nonvar", result))));
            DeclareMacro("arg_min", 4,
                (arg, score, generator, result) => {
                    var temp = Variable.MakeGenerated("*temp*");
                    return Or(And(new Call("initialize_variable", result),
                                  new Call("initialize_variable", temp),
                                  new Call("initialize_variable", score),
                                  generator,
                                  new Call("aggregate_argmin", score, temp, arg, result),
                                  Symbol.Fail),
                              new Call("nonvar", result));
                });
            DeclareMacro("arg_max", 4,
                 (arg, score, generator, result) => {
                     var temp = Variable.MakeGenerated("*temp*");
                     return Or(And(new Call("initialize_variable", result),
                                   new Call("initialize_variable", temp),
                                   new Call("initialize_variable", score),
                                   generator,
                                   new Call("aggregate_argmax", score, temp, arg, result),
                                   Symbol.Fail),
                               new Call("nonvar", result));
                });
            DeclareMacro("sum", 3,
                            (score, generator, result) => And(new Call("initialize_accumulator", result),
                                                              Or(And(generator,
                                                                     new Call("aggregate_sum", score, result),
                                                                     Symbol.Fail),
                                                                 Symbol.TruePredicate)));

            DeclareMacro("set", 1, arg =>
            {
                var ac = arg as Call;
                if (ac == null)
                    throw new SyntaxError("Invalid set command syntax", arg);
                if (Call.IsFunctor(ac, Symbol.Equal, 2))
                    return ExpandFunctionUpdate("update", ac.Arguments[0], ac.Arguments[1]);
                if (Call.IsFunctor(ac, Symbol.PlusEqual, 2))
                    return ExpandFunctionUpdate("increment", ac.Arguments[0], ac.Arguments[1]);
                throw new SyntaxError("Invalid set command syntax", arg);
            });

            DeclareMacro("setof", 2, (input, output) =>
            {
                var c = input as Call;
                if (c == null || !c.IsFunctor(Symbol.Colon, 2))
                    throw new ArgumentException("Argument to setof must be of the form Var:Generator.");
                var v = c.Arguments[0];
                var generator = c.Arguments[1];
                return And(new Call(Symbol.Equal, output, new Call(Symbol.Hashset)),
                            Or(And(generator,
                                   new Call(Symbol.Adjoin, output, v),
                                   Symbol.Fail),
                               true));
            });

            DeclareMacro("listof", 2, (input, output) =>
            {
                var c = input as Call;
                if (c == null || !c.IsFunctor(Symbol.Colon, 2))
                    throw new ArgumentException("Argument to setof must be of the form Var:Generator.");
                var v = c.Arguments[0];
                var generator = c.Arguments[1];
                return And(new Call(Symbol.Equal, output, new Call(Symbol.ArrayList)),
                            Or(And(generator, 
                                   new Call(Symbol.Adjoin, output, v),
                                   Symbol.Fail),
                               true));
            });
        }

        #region Macro declation
        public static void DeclareMacro(string name, int arity, Func<object, object> expander)
        {
            DeclareMacro(name, arity, (Delegate)expander);
        }

        public static void DeclareMacro(string name, int arity, Func<object, object, object> expander)
        {
            DeclareMacro(name, arity, (Delegate)expander);
        }

        public static void DeclareMacro(string name, int arity, Func<object, object, object, object> expander)
        {
            DeclareMacro(name, arity, (Delegate)expander);
        }

        public static void DeclareMacro(string name, int arity, Func<object, object, object, object, object> expander)
        {
            DeclareMacro(name, arity, (Delegate)expander);
        }

        public static void DeclareMacro(string name, int arity, Delegate expander)
        {
            Macrotable[new PredicateIndicator(Symbol.Intern(name), arity)] = expander;
        }

        internal static readonly Dictionary<PredicateIndicator, Delegate> Macrotable = new Dictionary<PredicateIndicator, Delegate>();
        #endregion

        #region Term construction utilities
        public static object And(params object[] arguments)
        {
            return MakeConnective(Symbol.Comma, arguments);
        }

        public static object Or(params object[] arguments)
        {
            return MakeConnective(Symbol.Disjunction, arguments);
        }

        public static object Not(object o)
        {
            return new Call(Symbol.Intern("not"), o);
        }

        private static object MakeConnective(Symbol functor, object[] arguments)
        {
            var result = arguments[0];
            for (int i = 1; i < arguments.Length; i++)
                result = new Call(functor, result, arguments[i]);
            return result;
        }
        #endregion

        #region Function macro expansion
        private static object ExpandFunctionUpdate(string updatePredicate, object functionArg, object newValueArg)
        {
            if (!Functions.IsFunctionRelation(functionArg))
                throw new InvalidOperationException("Unknown function in set expression: "+functionArg);

            var c = functionArg as Call;
            // ReSharper disable once PossibleNullReferenceException
            var expandedArgs = new object[c.Arity + 1];
            for (var i = 0; i < c.Arguments.Length; i++)
                expandedArgs[i] = c.Arguments[i];
            expandedArgs[expandedArgs.Length - 1] = newValueArg;
            return new Call(updatePredicate, new Call(c.Functor, expandedArgs));
        }
        #endregion
    }
}
