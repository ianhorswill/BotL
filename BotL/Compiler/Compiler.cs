#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Compiler.cs" company="Ian Horswill">
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
using System.Linq;
using BotL.Parser;
using BotL.Unity;
using UnityEngine;

namespace BotL.Compiler
{
    public static class Compiler
    {
        /// <summary>
        ///  The goal currently being compiled
        /// </summary>
        public static object CurrentGoal { get; set; }
        public static object CurrentTopLevelExpression { get; private set; }


        public const int MaxSpecialPredicateArity=10;
        static Compiler()
        {
            Macros.DeclareMacros();
            
            SpecialClauses = new CompiledClause[MaxSpecialPredicateArity];
            for (int arity = 0; arity < SpecialClauses.Length; arity++)
            {
                SpecialClauses[arity] = MakeSpecialClause(arity);
            }
        }

        #region Entry points
        public static void Compile(string s)
        {
            CompileStream(new ExpressionParser(s));
        }

        private static void CompileStream(ExpressionParser expressionParser, bool requireDelimiters= false)
        {
            while (!expressionParser.EOF)
            {
                CompileInternal(expressionParser.Read());
                expressionParser.SwallowStatementDeliminters(requireDelimiters);
            }
        }

        static readonly HashSet<string> LoadedSourceFiles = new HashSet<string>();
        static string CanonicalizeSourceName(object name)
        {
            var path = name as string;
            if (path == null)
            {
                if (name is Symbol)
                    path = ((Symbol) name).Name;
                else throw new ArgumentException("Invalid source module name: "+name);
            }

            if (string.IsNullOrEmpty(Path.GetExtension(path)))
                path = path + ".bot";
            return UnityUtilities.CanonicalizePath(path);
        }

        public static void CompileFile(string path)
        {
            var canonical = CanonicalizeSourceName(path);
            using (var f = File.OpenText(canonical))
            {
                var reader = new PositionTrackingTextReader(f, path);
                try
                {
                    LoadedSourceFiles.Add(canonical);
                    CompileStream(new ExpressionParser(reader), true);
                }
                catch (Exception e)
                {
                    if (Repl.StandardError != null)
                        Repl.StandardError.WriteLine($"{path}:{reader.Line}: {e.Message}");
                    throw;
                }
            }
        }

        public static void Compile(params string[] code)
        {
            foreach (var s in code)
                Compile(s);
        }

        public static void Compile(object assertion)
        {
            CompileInternal(assertion);
        }

        private static bool ProcessDeclaration(object maybeDeclaration)
        {
            var c = maybeDeclaration as Call;
            if (c == null || c.Arity != 1)
                return false;
            var arg = c.Arguments[0];
            switch (c.Functor.Name)
            {
                case "function":
                    Functions.DeclareFunction(DecodePredicateIndicatorExpression(arg));
                    break;

                case "table":
                    var path = arg as string;
                    if (path != null)
                        KB.LoadTable(path);
                    else
                        KB.DefineTable(DecodePredicateIndicatorExpression(arg));
                    break;

                case "require":
                    var module = arg as string;
                    if (module == null)
                    {
                        if (arg is Symbol s)
                            module = s.Name;
                        else
                            throw new ArgumentException($"Invalid file name in require command: {ExpressionParser.WriteExpressionToString(arg)}");
                    }
                    if (!LoadedSourceFiles.Contains(CanonicalizeSourceName(module)))
                    {
                        CompileFile(module);
                    }
                    break;

                case "global":
                {
                    var g = arg as Call;
                    if (g == null || !g.IsFunctor(Symbol.Equal, 2) || !(g.Arguments[0] is Symbol))
                        throw new SyntaxError("Invalid global declaration", g);
                    GlobalVariable.DefineGlobal(((Symbol) g.Arguments[0]).Name, g.Arguments[1]);
                    break;
                }

                case "report":
                {
                    var s = arg as Symbol;
                    if (s == null)
                        throw new SyntaxError("Argument to report should be the name of a global variable", arg);
                    var g = GlobalVariable.Find(s);
                    g.ReportInStackDumps = true;
                    break;
                }

                case "struct":
                    Structs.DeclareStruct(arg);
                    break;
                    
                case "signature":
                {
                    var d = arg as Call;
                    if (d == null)
                        throw new SyntaxError("Malformed signature declaration", maybeDeclaration);
                    var p = KB.Predicate(new PredicateIndicator(d.Functor, d.Arity));
                    foreach (var t in d.Arguments)
                    {
                        if (!(t is Symbol))
                            throw new SyntaxError("Invalid type name " + t, maybeDeclaration);
                    }
                    p.Signature = d.Arguments.Cast<Symbol>().ToArray();
                }
                    break;

                case "trace":
                {
                    var spec = arg as Call;
                    if (spec == null || !spec.IsFunctor(Symbol.Slash, 2))
                        throw new ArgumentException("Invalid predicate specified in trace command");
                    KB.Predicate((Symbol) spec.Arguments[0], (int) spec.Arguments[1]).IsTraced = true;
                }
                    break;

                case "notrace":
                {
                    var spec = arg as Call;
                    if (spec == null || !spec.IsFunctor(Symbol.Slash, 2))
                        throw new ArgumentException("Invalid predicate specified in trace command");
                    KB.Predicate((Symbol) spec.Arguments[0], (int) spec.Arguments[1]).IsTraced = false;
                }
                    break;

                case "externally_called":
                    {
                        var spec = arg as Call;
                        if (spec == null || !spec.IsFunctor(Symbol.Slash, 2))
                            throw new ArgumentException("Invalid predicate specified in externally_called declaration");
                        KB.Predicate((Symbol)spec.Arguments[0], (int)spec.Arguments[1]).IsExternallyCalled = true;
                    }
                    break;

                case "listing":
                    {
                        var spec = arg as Call;
                        if (spec == null || !spec.IsFunctor(Symbol.Slash, 2))
                            throw new ArgumentException("Invalid predicate specified in externally_called declaration");
                        Listing(Repl.StandardOutput, KB.Predicate((Symbol)spec.Arguments[0], (int)spec.Arguments[1]));
                        break;
                    }

                default:
                    return false;
            }
            return true;
        }

        private static void Listing(TextWriter stream, Predicate p)
        {
            if (p.IsSpecial)
            {
                if (p.IsTable)
                    p.Table.Listing(stream);
                else
                    stream.WriteLine($"{p} is a primop");
            }
            else
                ListRulePredicate(stream, p);
        }

        private static void ListRulePredicate(TextWriter stream, Predicate p)
        {
            if (p.FirstClause != null)
            {
                ListClause(stream, p.FirstClause);
                foreach (var c in p.ExtraClauses)
                    ListClause(stream, c);
            } else
                stream.WriteLine($"{p} is rule predicate with no rules.");
        }

        private static void ListClause(TextWriter stream, CompiledClause clause)
        {
            stream.WriteLine(ExpressionParser.WriteExpressionToString(clause.Source));
        }

        private static PredicateIndicator DecodePredicateIndicatorExpression(object exp)
        {
            if (!Call.IsFunctor(exp, Symbol.Slash, 2))
                throw new SyntaxError("Malformed predicate indicator", exp);
            var args = ((Call) exp).Arguments;
            return new PredicateIndicator((Symbol)args[0], (int)args[1]);
        }

        internal static BindingEnvironment CompileInternal(object assertion, bool forceVoidVariables = false)
        {
            CurrentTopLevelExpression = assertion;
            CurrentGoal = null;
            if (ProcessDeclaration(assertion))
                return null;
            assertion = Transform.TransformTopLevel(assertion);
            CurrentGoal = null;
            BindingEnvironment e = new BindingEnvironment();
            assertion = Transform.Variablize(assertion, e);
            AnalyzeVariables(assertion, e);
            if (forceVoidVariables)
                e.IncrementVoidVariableReferences();
            AllocateVariables(assertion, e);

            CompiledClause clause;
            if (Call.IsFunctor(assertion, Symbol.Implication, 2))
            {
                var implication = assertion as Call;
                // ReSharper disable once PossibleNullReferenceException
                var head = implication.Arguments[0];
                var body = implication.Arguments[1];
                clause = CompileRule(assertion, head, body, e);
                Predicate.AddClause(new PredicateIndicator(head), clause);
            }
            else
            {
                clause = CompileFact(assertion, e);
                Predicate.AddClause(new PredicateIndicator(assertion), clause);
            }
            GenerateSingletonWarnings(clause, e);
            return e;
        }

        private static void GenerateSingletonWarnings(CompiledClause clause, BindingEnvironment e)
        {
            foreach (var v in e.Variables)
            {
                if (v.IsSingleton && !v.Variable.IsGenerated && !v.Variable.Name.Name.StartsWith("_"))
                    clause.AddWarning("Singleton variable {0} - might be a typo", v.Variable.Name);
            }
        }

        #endregion

        #region Compiling facts (rules with no body)
        private static readonly byte[] TrivialFactCode = { (byte)Opcode.CNoGoal };

        private static CompiledClause CompileFact(object head, BindingEnvironment e)
        {
            CurrentGoal = head;
            var spec = new PredicateIndicator(head);
            if (spec.Arity == 0)
                return new CompiledClause(head, TrivialFactCode, 0, null);

            var b = new CodeBuilder(KB.Predicate(spec));
            CompileArglist(head, b, e, true);
            b.Emit(Opcode.CNoGoal);
            return new CompiledClause(head, b.Code, e.EnvironmentSize, null);
        }
        #endregion

        #region Compiling rules
        private static CompiledClause CompileRule(object source, object head, object body, BindingEnvironment e)
        {
            var b = new CodeBuilder(KB.Predicate(new PredicateIndicator(head)));
            if (head is Variable)
                throw new SyntaxError("Head of clause may not be a variable", source);
            CompileHead(head, b, e);
            CompileGoal(body, b, e, true);

            if (body == Symbol.Cut)
                b.Emit(Opcode.CNoGoal);

            return new CompiledClause(source, b.Code, e.EnvironmentSize, MakeHeadModel(head, e));
        }

        private static object[] MakeHeadModel(object head, BindingEnvironment environment)
        {
            var c = head as Call;
            if (c == null)
                return null;
            var model = new object[c.Arity];
            for (int i = 0; i < model.Length; i++)
            {
                var v = c.Arguments[i] as Variable;
                if (v == null)
                    model[i] = c.Arguments[i];
                else
                {
                    model[i] = new StackReference(environment[v].EnvironmentIndex);
                }
            }
            return model;
        }

        private static void CompileHead(object head, CodeBuilder b, BindingEnvironment e)
        {
            CurrentGoal = head;
            CompileArglist(head, b, e, true);
            // Else c is a symbol, so there's nothing to compile.
        }

        private static void CompileArglist(object term, CodeBuilder b, BindingEnvironment e, bool isHead)
        {
            var c = term as Call;
            if (c != null)
            {
                foreach (var a in c.Arguments)
                {
                    CompileArgument(a, b, e, isHead);
                }
            }
        }

        private static void CompileArgument(object a, CodeBuilder b, BindingEnvironment e, bool isHead=false)
        {
            var v = a as Variable;
            if (v != null)
            {
                var variableInfo = e[v];
                if (variableInfo.Type == VariableType.Void)
                {
                    b.Emit(AdjustArgumentOpcode(Opcode.HeadVoid, isHead));
                }
                else if (variableInfo.FirstReferenceCompiled)
                {
                    b.Emit(AdjustArgumentOpcode(Opcode.HeadVarMatch, isHead));
                    b.Emit((byte) variableInfo.EnvironmentIndex);
                }
                else
                {
                    b.Emit(AdjustArgumentOpcode(Opcode.HeadVarFirst, isHead));
                    b.Emit((byte) variableInfo.EnvironmentIndex);
                    variableInfo.FirstReferenceCompiled = true;
                }
            }
            else
            {
                var ac = a as Call;
                var gv = a as GlobalVariable;
                if (ac != null)
                {
                    if (ac.Arity == 2 && ac.Functor == Symbol.Slash && ac.Arguments[0] is Symbol &&
                        ac.Arguments[1] is int)
                    {
                        // Special case: this is a literal referring to a predicate
                        b.EmitConstant(AdjustArgumentOpcode(Opcode.HeadConst, isHead),
                            KB.Predicate(new PredicateIndicator((Symbol) ac.Arguments[0], (int) ac.Arguments[1])));
                    }
                    else
                    {
                        // Otherwise it's a functional expression, so emit that.
                        CompileFunctionalExpressionArgument(a, b, e, isHead);
                    }
                } else if (gv != null)
                {
                    // It's a global varaible
                    CompileFunctionalExpressionArgument(a, b, e, isHead);
                }
                else
                    b.EmitConstant(AdjustArgumentOpcode(Opcode.HeadConst, isHead), a);
            }
        }

        private static void CompileFunctionalExpressionArgument(object a, CodeBuilder b, BindingEnvironment e, bool isHead)
        {
            b.Emit(AdjustArgumentOpcode(Opcode.HeadConst, isHead));
            b.Emit((byte) OpcodeConstantType.FunctionalExpression);
            CompileFunctionalExpression(a, b, e);
            b.Emit(FOpcode.Return);
        }

        /// <summary>
        /// Call with a head opcode and a boolean indicating whether it's intended for the head.
        /// If not, it will change it to the associated goal opcode.
        /// </summary>
        /// <param name="o">Opcode for a head instruction</param>
        /// <param name="isHead">True if opcode is to be inserted in head, false if it's to be inserted in a goal call.</param>
        /// <returns>The adjusted opcode</returns>
        private static Opcode AdjustArgumentOpcode(Opcode o, bool isHead)
        {
            if (isHead)
                return o;
            return (Opcode)(((int)o+1)*8);
        }
        
        private static readonly object[] EmptyHeadModel = new object[0];
        
        private static void CompileGoal(object goal, CodeBuilder b, BindingEnvironment e, bool lastCall)
        {
            CurrentGoal = goal;
            Call c = goal as Call;

            if (goal is Variable)
                throw new SyntaxError("Goal may not be a variable", goal);
            if (goal == Symbol.Cut)
            {
                b.Emit(Opcode.CCut);
                if (lastCall)
                    b.Emit(Opcode.CNoGoal);
            }
            else if (c != null && c.IsFunctor(Symbol.Comma, 2))
            {
                // ReSharper disable once PossibleNullReferenceException
                CompileGoal(c.Arguments[0], b, e, false);
                CompileGoal(c.Arguments[1], b, e, lastCall);
            } else if (c != null && c.IsFunctor(Symbol.Disjunction, 2))
            {
                var nested = new Predicate(Symbol.Intern("*or*"), 0) {IsNestedPredicate = true};
                CompileDisjuncts(goal, b, e, nested);
                nested.ImportConstantTablesFrom(b.Predicate);
                b.EmitGoal(nested);
                b.Emit(lastCall ? Opcode.CLastCall : Opcode.CCall);
            } else if (c != null && c.Functor == Symbol.Call)
            {
                CompileMetaCall(c, b, e, lastCall);
            } else if (goal == Symbol.Fail || goal.Equals(false))
            {
                b.EmitBuiltin(Builtin.Fail);
            }
            else if (c != null && BuiltinTable.BuiltinOpcode(c).HasValue)
                CompileBuiltin(c, b, e, lastCall);
            else
            {
                b.EmitGoal(KB.Predicate(new PredicateIndicator(goal)));
                CompileArglist(goal, b, e, false);
                // Else c is a symbol, so there's nothing to compile.
                b.Emit(lastCall ? Opcode.CLastCall : Opcode.CCall);
            }
        }

        private static void CompileBuiltin(Call c, CodeBuilder b, BindingEnvironment e, bool lastCall)
        {
            // ReSharper disable once PossibleInvalidOperationException
            var builtin = BuiltinTable.BuiltinOpcode(c).Value;
            switch (builtin)
            {
                case Builtin.Var:
                case Builtin.NonVar:
                {
                    if (c.Arguments[0] is Variable v)
                    {
                        if (e[v].FirstReferenceCompiled)
                        {
                            b.EmitBuiltin(builtin);
                            b.Emit((byte) e[v].EnvironmentIndex);
                        }
                        // This is the first use of the variable; it can't be instantiated.
                        else if (builtin == Builtin.NonVar)
                                b.EmitBuiltin(Builtin.Fail);
                        }
                    // Else it's a compile-time constant, so never var
                    else if (builtin == Builtin.Var)
                            // Always fail
                            b.EmitBuiltin(Builtin.Fail);
                    // Always true, so no-op
                }
                    break;

                case Builtin.UnsafeSet:
                    {
                    if (!(c.Arguments[0] is Variable lhs) || !(c.Arguments[1] is Variable rhs))
                        throw new InvalidOperationException("Arguments to unsafe_set must be variables.");
                    var lhsi = e[lhs];
                    var rhsi = e[rhs];
                    if (!rhsi.FirstReferenceCompiled || !lhsi.FirstReferenceCompiled)
                        throw new InvalidOperationException("Argument to unsafe_set is uninstantiated variable");
                    b.EmitBuiltin(builtin);
                    b.Emit((byte) lhsi.EnvironmentIndex);
                    b.Emit((byte) rhsi.EnvironmentIndex);
                }
                    break;

                case Builtin.UnsafeInitialize:
                case Builtin.UnsafeInitializeZero:
                {
                    if (!(c.Arguments[0] is Variable v))
                        throw new InvalidOperationException("Argument to unsafe_initialize is not a variable");
                    var vi = e[v];
                    if (!vi.FirstReferenceCompiled)
                    {
                        b.EmitBuiltin(builtin);
                        b.Emit((byte) vi.EnvironmentIndex);
                        vi.FirstReferenceCompiled = true;
                    }
                    // Else nop
                }
                    break;

                case Builtin.MaximizeUpdate:
                case Builtin.MaximizeUpdateAndRepeat:
                case Builtin.MinimizeUpdate:
                case Builtin.MinimizeUpdateAndRepeat:
                case Builtin.SumUpdateAndRepeat:
                    {
                    if (!(c.Arguments[0] is Variable lhs) || !(c.Arguments[1] is Variable rhs))
                        throw new InvalidOperationException("Arguments to maximize/minimize/sum_update must be variables.");
                    var lhsi = e[lhs];
                    var rhsi = e[rhs];
                    if (!rhsi.FirstReferenceCompiled || !lhsi.FirstReferenceCompiled)
                        throw new InvalidOperationException("Argument to maximize/minimize/sum_update is uninstantiated variable");
                    b.EmitBuiltin(builtin);
                    b.Emit((byte) lhsi.EnvironmentIndex);
                    b.Emit((byte) rhsi.EnvironmentIndex);
                    break;
                }

                case Builtin.LessThan:
                case Builtin.GreaterThan:
                case Builtin.LessEq:
                case Builtin.GreaterEq:
                    {
                    b.EmitBuiltin(builtin);
                    CompileFunctionalExpression(c.Arguments[0], b, e);
                    b.Emit(FOpcode.Return);
                    CompileFunctionalExpression(c.Arguments[1], b, e);
                    b.Emit(FOpcode.Return);
                    break;
                }

                case Builtin.IntegerTest:
                case Builtin.FloatTest:
                case Builtin.NumberTest:
                case Builtin.StringTest:
                case Builtin.SymbolTest:
                case Builtin.MissingTest:
                case Builtin.TestNotFalse:
                case Builtin.Throw:
                    {
                        var arg = c.Arguments[0];
                        if (!(arg is Variable v) || e[v].FirstReferenceCompiled)
                        {
                            b.EmitBuiltin(builtin);
                            CompileFunctionalExpression(arg, b, e, false);
                            b.Emit(FOpcode.Return);
                        }
                        else
                        {
                            // argument is a variable known at compile-time to be uninstantiated.
                            b.EmitBuiltin(Builtin.Fail);
                        }
                        break;
                    }

                case Builtin.CallFailed:
                {
                    var arg = c.Arguments[0] as Call;
                    if (arg == null || !arg.IsFunctor(Symbol.Slash, 2))
                    {
                        throw new Exception("Argument to %call_failed must be a predicate selector: "+c.Arguments[0]);
                    }
                    var failedPredicate = KB.Predicate((Symbol) arg.Arguments[0], (int) arg.Arguments[1]);
                    b.EmitBuiltin(Builtin.CallFailed);
                    b.Emit(b.Predicate.GetObjectConstantIndex(failedPredicate));
                    break;
                }

                default:
                    throw new InvalidOperationException("Attempt to compile unknown builtin "+ builtin);
            }

            if (lastCall)
                b.Emit(Opcode.CNoGoal);
        }

        private static void CompileMetaCall(Call call, CodeBuilder b, BindingEnvironment e, bool lastCall)
        {
            var targetArity = call.Arity - 1;
            // Call the internal primop to look up the predicate
            b.EmitGoal(KB.Predicate(Symbol.Call, 2));
            CompileArgument(call.Arguments[0], b, e);
            CompileArgument(targetArity, b, e);
            b.Emit(Opcode.CCall);
            // When call returns, headPredicate has been reset to the thing we're calling, so fall through to the target's arglist.7
            for (var i = 1; i<call.Arguments.Length; i++)
                CompileArgument(call.Arguments[i], b, e);
            b.Emit(lastCall ? Opcode.CLastCall : Opcode.CCall);
        }

        #endregion

        #region Compiling nested clauses (disjunctions)
        private static readonly CompiledClause TrueDisjunctClause
            = new CompiledClause(true, TrivialFactCode, 0, null);
        private static void CompileDisjuncts(object goal, CodeBuilder b, BindingEnvironment e, Predicate nested)
        {
            if (goal.Equals(true) || goal == Symbol.TruePredicate)
                nested.AddClause(TrueDisjunctClause);
            else if (goal.Equals(false) || goal == Symbol.Fail)
            {
                // Don't do anything
            }
            else if (Call.IsFunctor(goal, Symbol.Disjunction, 2))
            {
                nested.AddClause(CompileDisjunctClause(b, e, ((Call) goal).Arguments[0]));
                nested.AddClause(CompileDisjunctClause(b, e, ((Call) goal).Arguments[1]));
            }
            else
                nested.AddClause(CompileDisjunctClause(b, e, goal));
        }

        private static CompiledClause CompileDisjunctClause(CodeBuilder b, BindingEnvironment e, object disjunct)
        {
            var b1 = new CodeBuilder(b.Predicate);
            CompileGoal(disjunct, b1, e, true);
            var compiledClause = new CompiledClause(disjunct, b1.Code, e.EnvironmentSize, EmptyHeadModel);
            return compiledClause;
        }
        #endregion

        #region Functional Expressions
        /// <summary>
        /// Emits code for functional expression EXP.  Does not emit the preamble or return instruction.
        /// </summary>
        private static void CompileFunctionalExpression(object exp, CodeBuilder b, BindingEnvironment e, bool checkInstantiation=true)
        {
            var v = exp as Variable;
            var c = exp as Call;
            var g = exp as GlobalVariable;
            if (g != null)
            {
                b.Emit(FOpcode.LoadGlobal);
                b.Emit(b.Predicate.GetObjectConstantIndex(exp));
            }
            else if (v != null)
            {
                // It's a variable reference
                b.Emit(checkInstantiation?FOpcode.Load:FOpcode.LoadUnchecked);
                var variableInfo = e[v];
                if (variableInfo.Type == VariableType.Void || !variableInfo.FirstReferenceCompiled)
                {
                    throw new InvalidOperationException($"Reference to uninstantiated variable {v.Name} in functional expression.");
                }

                b.Emit((byte) variableInfo.EnvironmentIndex);
            }
            else if (c != null)
            {
                if (c.IsFunctor(Symbol.Dot, 2))
                    CompileMemberReference(c.Arguments[0], c.Arguments[1], b, e);
                else if (c.IsFunctor(Symbol.ColonColon, 2))
                    CompileComponentReference(c.Arguments[0], c.Arguments[1], b, e);
                else if (c.IsFunctor(Symbol.New, 1))
                {
                    // It's a new Type(args) expression
                    var arg = c.Arguments[0] as Call;
                    if (arg == null)
                    {
                        throw new SyntaxError("Malformed new expression", c.Arguments[0]);
                    }
                    var type = TypeUtils.FindType(arg.Functor.Name);
                    if (type == null)
                        throw new Exception("Unknown type " + arg.Functor);
                    CompileFunctionalExpression(type, b, e); // Push type name on stack

                    foreach (var a in arg.Arguments)
                    {
                        CompileFunctionalExpression(a, b, e);
                    }
                    b.Emit(FOpcode.Constructor);
                    b.Emit((byte) arg.Arity);
                }
                else
                {
                    // It's a function call
                    var fOpcode = FOpcodeTable.Opcode(c);
                    if (FOpcodeTable.ReverseArguments(fOpcode))
                        for (int i = c.Arguments.Length - 1; i >= 0; i--)
                        {
                            CompileFunctionalExpression(c.Arguments[i], b, e);
                        }
                    else
                        foreach (var a in c.Arguments)
                        {
                            CompileFunctionalExpression(a, b, e);
                        }
                    b.Emit(fOpcode);
                    if (fOpcode == FOpcode.Array || fOpcode == FOpcode.ArrayList || fOpcode == FOpcode.Queue ||
                        fOpcode == FOpcode.Hashset)
                        b.Emit((byte) c.Arity);
                }
            }
            else
            {
                // It's a constant
                if (exp is int)
                {
                    var i = (int) exp;
                    if (Math.Abs(i) < 128)
                    {
                        b.Emit(FOpcode.PushSmallInt);
                        b.Emit((byte) i);
                    }
                    else
                    {
                        b.Emit(FOpcode.PushInt);
                        b.Emit(b.Predicate.GetIntConstantIndex(i));
                    }
                } else if (exp is float)
                {
                    b.Emit(FOpcode.PushFloat);
                    b.Emit(b.Predicate.GetFloatConstantIndex((float) exp));
                }
                else if (exp is bool)
                {
                    b.Emit(FOpcode.PushBoolean);
                    b.Emit(b.Predicate.GetFloatConstantIndex((bool)exp?1:0));
                }
                else
                {
                    b.Emit(FOpcode.PushObject);
                    b.Emit(b.Predicate.GetObjectConstantIndex(exp));
                }
            }

        }

        private static void CompileMemberReference(object objectExpression, object member, CodeBuilder b, BindingEnvironment e)
        {
            CompileFunctionalExpression(objectExpression,  b, e);
            var fieldName = member as Symbol;
            if (fieldName != null)
            {
                // It's a field reference
                CompileFunctionalExpression(fieldName.Name, b, e); // Push on stack
                b.Emit(FOpcode.FieldReference);
            }
            else
            {
                var c = member as Call;
                if (c == null)
                    throw new SyntaxError("Invalid member expression", member);
                CompileFunctionalExpression(c.Functor.Name, b, e); // Push method name on stack
                foreach (object arg in c.Arguments)
                    CompileFunctionalExpression(arg, b, e);
                b.Emit(FOpcode.MethodCall);
                b.Emit((byte) c.Arity);
            }
        }

        private static void CompileComponentReference(object objectExpression, object type, CodeBuilder b, BindingEnvironment e)
        {
            CompileFunctionalExpression(objectExpression, b, e);
            var typeName = type as Symbol;
            if (typeName != null)
            {
                CompileFunctionalExpression(TypeUtils.FindType(typeName.Name), b, e); // Push on stack
                b.Emit(FOpcode.ComponentLookup);
            }
            else
            {
                throw new SyntaxError("Invalid component expression", type);
            }
        }
        #endregion

        #region Variable analysis
        /// <summary>
        /// Count usages of variables in the term.
        /// </summary>
        private static void AnalyzeVariables(object term, BindingEnvironment e)
        {
            if (Call.IsFunctor(term, Symbol.Implication, 2))
            {
                var c = (Call) term;
                AnalyzeVariables(c.Arguments[0], e, true);
                AnalyzeVariables(c.Arguments[1], e, false);
            }
            else
            {
                AnalyzeVariables(term, e, true);
            }
        }

        private static void AnalyzeVariables(object term, BindingEnvironment e, bool isHead)
        {
            var v = term as Variable;
            if (v != null)
            {
                e[v].NoteUse(isHead);
            }
            else
            {
                var c = term as Call;
                if (c != null)
                {
                    foreach (var a in c.Arguments)
                        AnalyzeVariables(a, e, isHead);
                }
            }
        }

        private static void AllocateVariables(object term, BindingEnvironment e)
        {
            var v = term as Variable;
            if (v != null)
            {
                var variableInfo = e[v];
                if (variableInfo.Type != VariableType.Void && variableInfo.EnvironmentIndex < 0)
                    e.AllocateSlot(variableInfo);
            }
            else
            {
                var c = term as Call;
                if (c != null)
                {
                    foreach (var a in c.Arguments)
                    {
                        AllocateVariables(a, e);
                    }
                }
            }
        }
        #endregion

        #region Special predicates
        public delegate CallStatus PredicateImplementation(ushort argBase, ushort restartedNextClause);
        internal static Predicate MakePrimop(Symbol name, int arity, PredicateImplementation implementation, byte tempVars = 0)
        {
            return new Predicate(name, arity, implementation, null, tempVars) { FirstClause = SpecialClauses[arity] };
        }

        private static readonly CompiledClause[] SpecialClauses;

        private static CompiledClause MakeSpecialClause(int arity)
        {
            var code = new byte[2 * arity + 1];
            for (int i = 0; i < arity; i++)
            {
                code[2 * i] = (byte)Opcode.HeadVarFirst;
                code[2 * i + 1] = (byte)i;
            }
            code[code.Length - 1] = (byte)Opcode.CSpecial;
            var headModel = new object[arity];
            for (int i = 0; i < headModel.Length; i++)
            {
                headModel[i] = new StackReference(i);
            }
            return new CompiledClause(null, code, (ushort)arity, headModel);
        }

        internal static Predicate MakeTable(Symbol name, int arity)
        {
            return new Predicate(name, arity, null, new Table(name, arity))
            {
                FirstClause = SpecialClauses[arity]
            };
        }

        internal static Predicate LoadTable(string path)
        {
            using (var file = File.OpenText(UnityUtilities.CanonicalizePath(path)))
            {
                var parser = new CSVParser(',', new PositionTrackingTextReader(file, path));
                var predicateName = Symbol.Intern(Path.GetFileNameWithoutExtension(path));
                var rows = new List<object[]>();
                parser.Read((rowNum, row) => rows.Add(row));
                if (rows.Count == 0)
                    throw new Exception("No rows found in table file");
                var arity = rows[0].Length;
                var table = new Table(predicateName, arity);
                table.AddRows(rows);
                if (parser.Signature.Count != rows[0].Length)
                {
                    // At least one column must be a struct
                    // So do an implicit signature declaration
                    KB.Predicate(new PredicateIndicator(predicateName, parser.Signature.Count)).Signature =
                        parser.Signature.ToArray();
                }
                return new Predicate(predicateName, arity, null, table)
                {
                    FirstClause = SpecialClauses[arity]
                };
            }
        }
        #endregion

        #region Compiler-internal helper predicates
#if NOTUSED
        internal static Predicate MakePredicateFromClauseCode(string name, int arity, byte[] code, object[] headModel)
        {
            return new Predicate(Symbol.Intern(name), arity)
            {
                FirstClause = new CompiledClause(null, code, (ushort)arity, headModel)
            };
        }

        private static readonly Predicate IsTrue =
            MakePredicateFromClauseCode("expression_is_true", 1,
                new byte[]
                {
                    (byte) Opcode.HeadConst, // Compare first arg to true
                    (byte) OpcodeConstantType.Boolean,
                    1, // true
                    (byte) Opcode.CNoGoal // return success
                },
                new object[] {Symbol.Intern("expression")});
#endif
        #endregion
    }
}
