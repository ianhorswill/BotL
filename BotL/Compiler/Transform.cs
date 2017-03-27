#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Transform.cs" company="Ian Horswill">
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
    /// Implements source-to-source transforms to put code in cannical form for compilation:
    /// - "Predicatization": transforming function(args) = value to function(args, value)
    /// - Macro expansion
    /// - Hoisting function predicates from arguments to goals.
    /// </summary>
    internal static class Transform
    {
        #region Code walk
        /// <summary>
        /// Transform a rule or fact.
        /// </summary>
        internal static object TransformTopLevel(object assertion)
        {
            if (Call.IsFunctor(assertion, Symbol.GrammarRule, 2))
                assertion = MacroExpand(assertion);

            var c = assertion as Call;
            if (c == null)
                return assertion;
            if (!c.IsFunctor(Symbol.Implication, 2))
            {
                assertion = TransformFact(assertion);
                if (Call.IsFunctor(assertion, Symbol.Implication, 2))
                    return TransformTopLevel(assertion);
                return assertion;
            }
            // It's a rule.
            var args = c.Arguments;
            var head = args[0];
            var body = args[1];
            var tHead = TransformHead(head);
            var tBody = TransformGoal(body);
            var hc = tHead as Call;
            if (hc != null && hc.IsFunctor(Symbol.Comma, 2))
            {
                args[0] = hc.Arguments[1];
                args[1] = new Call(Symbol.Comma, tBody, hc.Arguments[0]);
            }
            else
            {
                args[0] = tHead;
                args[1] = tBody;
            }
            return assertion;
        }

        /// <summary>
        /// Transform the head of a rule or fact.
        /// </summary>
        private static object TransformFact(object head)
        {
            var expanded = TransformHead(head);
            var c = expanded as Call;
            if (c != null && c.IsFunctor(Symbol.Comma, 2))
            {
                // it got hoisted, so take the hoisted code and put it on the RHS of a rule.
                return new Call(Symbol.Implication, c.Arguments[1], c.Arguments[0]);
            }
            return expanded;
        }

        private static object TransformHead(object head)
        {
            var expanded = HoistArguments(Canonicalize(head), true);
            return expanded;
        }

        /// <summary>
        /// Transform a goal
        /// </summary>
        private static object TransformGoal(object goal)
        {
            goal = Canonicalize(goal);
            var c = goal as Call;
            if (c == null)
                return goal;
            if (c.Arity == 2)
            {
                switch (c.Functor.Name)
                {
                    case ",":
                    case "|":
                        c.Arguments[0] = TransformGoal(c.Arguments[0]);
                        c.Arguments[1] = TransformGoal(c.Arguments[1]);
                        return goal;
                }
            }
            return TransformSimpleGoal(goal);
        }

        private static object TransformSimpleGoal(object goal)
        {
            // When we get here, goal is a call to a macro or predicate and has arguments.
            // Start by trying to expand any macros at top level.
            var expanded = MacroExpand(goal);
            if (expanded != goal)
                // We macroexpanded, so we might have to do arbitrary transforms on the result.
                return TransformGoal(expanded);
            // Goal had no macros, so all we have to do is hoist it.
            var hoisted = HoistArguments(goal);
            if (hoisted == goal)
                return goal;
            // We hoisted, so we have to go back and transform the result.
            return TransformGoal(hoisted);
        }

        #endregion

        #region Macro expansion
        private static object MacroExpand(object term)
        {
            Delegate macro;
            while (Macros.Macrotable.TryGetValue(new PredicateIndicator(term), out macro))
            {
                var c = term as Call;
                term = c != null ? macro.DynamicInvoke(c.Arguments) : macro.DynamicInvoke();
            }
            return term;
        }
        #endregion

        #region Function call hoisting
        private static object HoistArguments(object term, bool fullyHoist = false)
        {
            var c = term as Call;
            if (c == null)
                return term;

            var result = term;
            for (var i = 0; i < c.Arguments.Length; i++)
            {
                var unhoisted = c.Arguments[i];
                object residue;
                var hoisted = Hoist(unhoisted, out residue);
                if (residue != null)
                {
                    if (fullyHoist && !(hoisted is Variable))
                    {
                        var t = Variable.MakeGenerated("*Hoisted*");
                        residue = new Call(Symbol.Comma, residue, new Call(Symbol.Equal, t, hoisted));
                        hoisted = t;
                    }
                    c.Arguments[i] = hoisted;
                    result = new Call(Symbol.Comma, residue, result);
                }
            }
            return result;
        }

        private static object Hoist(object unhoisted, out object residue)
        {
            var c = unhoisted as Call;
            if (c == null)
            {
                residue = null;
                return unhoisted;
            }
            if (Functions.IsFunctionRelation(c))
            {
                // It's hoistable.
                var t = Variable.MakeGenerated("*T*");
                residue = c.AddArgument(t);
                return t;
            }
            residue = null;
            // The top-level expression isn't hoistable  but the args might be.
            for (var i = 0; i < c.Arguments.Length; i++)
            {
                object subresidue;
                var un = c.Arguments[i];
                object hoisted = Hoist(un, out subresidue);
                if (subresidue != null)
                {
                    c.Arguments[i] = hoisted;
                    residue = (residue == null) ? subresidue : new Call(Symbol.Comma, residue, subresidue);
                }
            }
            return c;
        }
        #endregion

        #region Canonicalization
        private static object Canonicalize(object literal)
        {
            return ExpandStructs(Predicatize(literal));
        }

        private static object ExpandStructs(object literal)
        {
            var c = literal as Call;
            if (c == null)
                return literal;
            var sig = KB.Predicate(c.Functor, c.Arity).Signature;
            if (sig == null)
                return literal;
            List<object> expandedArglist = new List<object>();
            for (int i = 0; i < sig.Length; i++)
                switch (sig[i].Name)
                {
                    case "int":
                    case "integer":
                    case "float":
                    case "number":
                    case "string":
                    case "symbol":
                    case "object":
                        expandedArglist.Add(c.Arguments[i]);
                        break;

                    default:
                        Structs.FlattenInto(c.Arguments[i], sig[i], expandedArglist);
                        break;
                }
            return new Call(c.Functor, expandedArglist.ToArray());
        }

        /// <summary>
        /// If term is of the form Function = Value, turn it back into a call to the predicate underlying Function.
        /// Also, if term is a method call or field reference, wrap it appropriately.
        /// </summary>
        private static object Predicatize(object literal)
        {
            var c = literal as Call;
            if (c == null)
            {
                if (Equals(literal, true))
                    return Symbol.TruePredicate;
                if (Equals(literal, false))
                    return Symbol.Fail;
                return literal;
            }
            if (c.IsFunctor(Symbol.Equal, 2))
            {
                if (Functions.IsFunctionRelation(c.Arguments[0]))
                    return Call.AddArgument(c.Arguments[0], c.Arguments[1]);
                if (Functions.IsFunctionRelation(c.Arguments[1]))
                    return Call.AddArgument(c.Arguments[1], c.Arguments[0]);
            } else if (c.IsFunctor(Symbol.Dot, 2))
            {
                //if (c.Arguments[1] is Symbol)
                //{
                //    // It's a field reference
                //    return new Call("%is_true", literal);
                //}
                return new Call("%not_false", literal);
            }
            return literal;
        }
        #endregion

        #region Variablization
        // The parser reads the code in without distinguishing between symbols and variables.
        // So we have to talk the code and convert upper case symbols to variables.

        /// <summary>
        /// Rewrite any symbols in the term whose names are valid variable names with variables.
        /// This prevents the parser from having to track a binding environment.
        /// </summary>
        /// <param name="term">Term to rewrite</param>
        /// <param name="e">Binding environment for the rule or fact</param>
        /// <returns>Rewritten term</returns>
        internal static object Variablize(object term, BindingEnvironment e)
        {
            if (IsVariableName(term))
                return e[(Symbol)term];
            var c = term as Call;
            if (c != null)
            {
                if (c.IsFunctor(Symbol.DollarSign, 1))
                    return NamedEntities.Resolve(c.Arguments[0]);
                if (c.IsFunctor(Symbol.Dot, 2) && c.Arguments[1] is Symbol)
                {
                    // It's a field reference; don't variablize the second argument.
                    c.Arguments[0] = Variablize(c.Arguments[0], e);
                }
                else if (c.IsFunctor(Symbol.ColonColon, 2) && c.Arguments[1] is Symbol)
                {
                    // It's a field reference; don't variablize the second argument.
                    c.Arguments[0] = Variablize(c.Arguments[0], e);
                }
                else
                {
                    for (var i = 0; i < c.Arguments.Length; i++)
                        c.Arguments[i] = Variablize(c.Arguments[i], e);
                }
            }
            return term;
        }

        /// <summary>
        /// Term is a variable name
        /// </summary>
        public static bool IsVariableName(object o)
        {
            var s = o as Symbol;
            return s != null && IsVarChar(s.Name[0]);
        }

        /// <summary>
        /// Is a valid first character for a variable name.
        /// </summary>
        private static bool IsVarChar(char c)
        {
            return c == '_' || Char.IsUpper(c);
        }
        #endregion
    }
}
