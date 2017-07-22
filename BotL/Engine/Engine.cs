#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Engine.cs" company="Ian Horswill">
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
using System.Diagnostics;
using System.Reflection;
using BotL.Compiler;
using BotL.Parser;
using static BotL.Repl;

namespace BotL
{
    public static class Engine
    {
        /// <summary>
        /// How much space to reserve for the DataStack array
        /// </summary>
        private const int DStackSize = 2000;
        /// <summary>
        /// Size for the EnvironmentStack array
        /// </summary>
        private const int EStackSize = DStackSize / 2;
        /// <summary>
        /// Size for the ChoicePointStack array
        /// </summary>
        private const int CStackSize = DStackSize / 3;
        /// <summary>
        /// Size for the UndoStack array
        /// </summary>
        private const int UStackSize = DStackSize / 2;
        /// <summary>
        /// Holds arguments for predicates.
        /// Environment stack points into here to specify where the arguments of a given invocation are stored.
        /// </summary>
        internal static readonly TaggedValue[] DataStack = new TaggedValue[DStackSize];
        /// <summary>
        /// Activation frames for individual predicate invocations.
        /// Arguments for the invocation are stored in the DataStack,
        /// at the offset specified by the base address in this frame.
        /// Restart information for backtracking an invocation is stored in the ChoicePointStack.
        /// </summary>
        internal static readonly Environment[] EnvironmentStack = new Environment[EStackSize];
        /// <summary>
        /// Stack of restart information for invocations of backtrackable predicates in the environment stack.
        /// Each entry in the environment stack has at most one entry in the choicepointstack.  However,
        /// many calls are deterministic, meaning they can't be restarted, and so they have no entry here.
        /// As a consequence, when backtracking occurs, the system proceeds directly to whatever previous
        /// predicate *was* restartable.
        /// </summary>
        static readonly ChoicePoint[] ChoicePointStack = new ChoicePoint[CStackSize];
        /// <summary>
        /// Log of variables that have been bound to values and which will consequently need to be unbound
        /// if backtracking in the future.  A choicepoint specifies how far down the tail needs to be unwound
        /// if we restart that choicepoint.
        /// </summary>
        private static readonly ushort[] Trail = new ushort[DStackSize];
        /// <summary>
        /// The top of the trail (which is ultimately a stack).
        /// This is a field rather than a local variable of Engine.Run() because code outside of Run() needs
        /// to be able to bind variables, and hence to push the trail.
        /// </summary>
        internal static ushort TrailTop;
        /// <summary>
        /// A stack of operations to perform when backtracking.  So like the Trail, but for operations other
        /// than resetting variable bindings.  The trail is a specialized undo stack.
        /// Each choicepoint records how far down to unwind the undo stack.
        /// We assume that undoing is a rarer operation than trailing, so it's somewhat more expensive.
        /// </summary>
        internal static readonly UndoRecord[] UndoStack = new UndoRecord[UStackSize];
        /// <summary>
        /// Top of the undo stack.  Like the trail top, this is a field rather than a local variable of Run()
        /// because primops need to be able to push undo records.
        /// </summary>
        internal static ushort UTop;

#if DEBUG
        /// <summary>
        /// We are single-stepping the VM.  Don't set this unless you are using the REPL and want to
        /// watch the VM run instruction by instruction.
        /// </summary>
        public static bool SingleStep;
#endif

        #region Front end overloads of Run
        /// <summary>
        /// The name of the scratch predicate used by the repl to compile top-level queries into.
        /// </summary>
        private static readonly Symbol TopLevelGoal = Symbol.Intern("top_level_goal");
        /// <summary>
        /// The compiler's model of the environment for the top-level goal.
        /// This is used by TopLevelResultBindings to return the values of variables from the last
        /// call to Run(string goal).  It is not used otherwise.
        /// </summary>
        private static BindingEnvironment topLevelBindingEnvironment;

        /// <summary>
        /// Compile and run some code.
        /// </summary>
        /// <param name="goal">Source code to compile and run.</param>
        /// <returns>True if the query succeeded.</returns>
        public static bool Run(string goal)
        {
            KB.Predicate(new PredicateIndicator(TopLevelGoal, 0)).Clear();
            topLevelBindingEnvironment = Compiler.Compiler.CompileInternal(new ExpressionParser(TopLevelGoal.Name+" <-- "+goal).Read(), forceVoidVariables: true);
            return Run(TopLevelGoal);
        }

        /// <summary>
        /// Iterator to return the bindings of all the variables in the last query run by Run(string goal).
        /// </summary>
        public static IEnumerable<KeyValuePair<Symbol, object>> TopLevelResultBindings
        {
            get
            {
                foreach (var b in topLevelBindingEnvironment.FrameBindings(0))
                    yield return b;
            }
        }

        /// <summary>
        /// Runs an already-compiled predicate.  The predicate may not take arguments.
        /// </summary>
        /// <param name="entryPoint">Symbol naming the predicate.</param>
        /// <returns>True if the predicate succeeded.</returns>
        public static bool Run(Symbol entryPoint)
        {
            return Run(KB.Predicate(new PredicateIndicator(entryPoint)));
        }

        private static readonly Predicate TopLevelApply = KB.Predicate(Symbol.Intern("%apply_top_level"), 0);
        private static readonly Predicate TopLevelApplyFunction = KB.Predicate(Symbol.Intern("%apply_top_level_function"), 0);

        public static bool Apply(Symbol functor, params object[] arguments)
        {
            KB.TopLevelPredicate.Value.SetReference(functor);
            KB.TopLevelArgV.Value.SetReference(arguments);
            return Run(TopLevelApply);
        }

        public static bool Apply(string functor, params object[] arguments)
        {
            return Apply(Symbol.Intern(functor), arguments);
        }

        public static T ApplyFunction<T>(Symbol functor, params object[] arguments)
        {
            KB.TopLevelPredicate.Value.SetReference(functor);
            KB.TopLevelArgV.Value.SetReference(arguments);
            if (!Run(TopLevelApplyFunction))
                throw new CallFailedException(functor);
            var result = KB.TopLevelReturnValue.Value.Value;
            if (result is T)
                return (T)result;
            throw new InvalidCastException($"Call to BotL predicate {functor} did not return a value of type {typeof(T).Name}");
        }

        public static T ApplyFunction<T>(string functor, params object[] arguments)
        {
            return ApplyFunction<T>(Symbol.Intern(functor), arguments);
        }

        #endregion

        #region Trampoline used to start user code
        //
        // Trampoline
        // This isn't a real predicate.  It's a hand-coded placeholder that is stored into the PC
        // when the VM starts up so that it will invoke the actual predicate the user is trying to run.
        //

        private static readonly byte[] Trampoline = { (byte)Opcode.CLastCall };

        /// <summary>
        /// The clause used to start execution.  This will only ever appear on the bottom of the environment
        /// stack.
        /// </summary>
        private static readonly CompiledClause TrampolineRule =
            new CompiledClause(null, Trampoline, 0, null, null, 0);

        /// <summary>
        /// This is just a placeholder predicate so that the TrampolineRule has a predicate it belongs to
        /// and hence goalPredicate will be non-null even for the fake frame at the bottom of the
        /// environment stack.
        /// </summary>
        static readonly Predicate TrampolinePredicate = new Predicate(Symbol.Intern("trampoline"), 0);
        #endregion

        #region Virutal machine interpreter
        /// <summary>
        /// Run a specified predicate without any arguments.
        /// 
        /// The VM interpreter is inside this method.
        /// 
        /// WARNING: this is not reentrant!  Don't call it from inside a primop or other C# code called
        /// from within BotL code.
        /// </summary>
        /// <param name="topLevelPredicate">Predicate to call</param>
        /// <returns>True if the predicate succeeds.</returns>
        private static bool Run(Predicate topLevelPredicate)
        {
            #region Startup
            // Data stack pointer
            ushort dTop = 0;
            // Environment stack pointer
            ushort eTop = 0;
            // Choicepoint stack pointer
            ushort cTop = 0;

            //
            // Information captured at the start of a call
            //

            // Position within the caller's bytecode of start of the call currently being initiated
            ushort startOfCall = 0;
            // Trail pointer as of the start of the current call
            ushort trailSave = 0;
            // Data stack pointer as of the start of call
            ushort dTopSave = 0;
            // Index within the predicate we just restarted of the specific clause we are now trying
            ushort restartedClauseNumber = 0;

            //
            // Undo information
            //
            TrailTop = 0;
            UTop = 0;

            // Caller predicate currently running, i.e. the current goal
            // This initial value for this is *not* topLevelPredicate because the latter
            // might have multiple clauses and if a predicate backtracks we need to temporarily restore
            // its caller.  So we set the goal to be a trampoline predicate that does nothing but call
            // whatever was stored in headPredicate without any arguments.
            Predicate goalPredicate = TrampolinePredicate;
            // Compiled bytecode for the specific clause (rule) of the goalPredicate being run.
            byte[] goalCode = Trampoline;
            // Current instruction within goalCode
            ushort goalPc = 0;

            // headPredicate is subgoal predicate we are currently trying to call.
            // This is initially the topLevelPredicate. It gets called by the trampoline.
            Predicate headPredicate = topLevelPredicate;

            // If the top-level predicate has no clauses, fail right now.
            if (headPredicate.FirstClause == null)
                return false;

            // The current CompiledClause that we're trying for the head (callee subgoal).
            var headRule = headPredicate.FirstClause;
            // The compiled bytecode from headRule.
            byte[] headCode = headRule.Code;
            // Current instruction within headCode
            ushort headPc = 0;

            //
            // Push the initial stack frame for the trampoline
            //

            // The EnvironmentStack frame for the current goal, i.e. the goalPredicate, which is generally
            // trying to call some subgoal, headPredicate, whose environment frame won't be set up until
            // we finish calling into the subgoal and it becomes goalPredicate.
            ushort goalFrame = eTop++;
            EnvironmentStack[goalFrame] = new Environment(goalPredicate, TrampolineRule, dTop, 0, cTop, 0);

            // cTop before the call the to current head.
            // The callee saves this value in the environment upon entry to a clause
            // and then resets cTop to it if/when they perform a cut.
            //
            // We can't just let the callee remember cTop at entry time because
            // the cp stack may or may not have a frame for the callee upon entry.
            ushort callerCp = 0;

            // Allocate new choice frame, if necessary
            if (headPredicate.ExtraClauses != null)
                ChoicePointStack[cTop++] = new ChoicePoint(goalFrame, startOfCall, headPredicate, 0, dTopSave, trailSave, UTop, eTop);
            #endregion

            #region DoBuiltin
            bool DoBuiltin(Predicate predicate, byte[] code, ref ushort pc, ushort frameBase)
            {
                var builtin = (Builtin) code[pc++];
                switch (builtin)
                {
                    case Builtin.Fail:
                        return false;

                    case Builtin.Var:
                        return DataStack[Deref(frameBase + code[pc++])].Type == TaggedValueType.Unbound;

                    case Builtin.NonVar:
                        return DataStack[Deref(frameBase + code[pc++])].Type != TaggedValueType.Unbound;

                    case Builtin.UnsafeInitialize:
                        Unbind(frameBase + code[pc++]);
                        return true;

                    case Builtin.UnsafeInitializeZero:
                        DataStack[frameBase + code[pc++]].Set(0f);
                        return true;

                    case Builtin.UnsafeInitializeZeroInt:
                        DataStack[frameBase + code[pc++]].Set(0);
                        return true;

                    case Builtin.UnsafeSet:
                    {
                        var lhs = code[pc++];
                        var rhs = code[pc++];
                        DataStack[frameBase + lhs] = DataStack[Deref(frameBase + rhs)];
                        return true;
                    }

                    case Builtin.MaximizeUpdate:
                    case Builtin.MaximizeUpdateAndRepeat:
                        {
                            var lhs = Deref(frameBase + code[pc++]); // Deref here is being paranoid
                            var rhs = Deref(frameBase + code[pc++]);
                            var rhsAsFloat = DataStack[rhs].AsFloat;
                            if (DataStack[lhs].Type == TaggedValueType.Unbound
                                || DataStack[lhs].AsFloat < rhsAsFloat)
                            {
                                DataStack[lhs].Set(rhsAsFloat);
                                return builtin == Builtin.MaximizeUpdate;
                            }
                            return false;
                        }

                    case Builtin.MinimizeUpdate:
                    case Builtin.MinimizeUpdateAndRepeat:
                        {
                            var lhs = Deref(frameBase + code[pc++]); // Deref here is being paranoid
                            var rhs = Deref(frameBase + code[pc++]);
                            var rhsAsFloat = DataStack[rhs].AsFloat;
                            if (DataStack[lhs].Type == TaggedValueType.Unbound
                                || DataStack[lhs].AsFloat > rhsAsFloat)
                            {
                                DataStack[lhs].Set(rhsAsFloat);
                                return builtin == Builtin.MinimizeUpdate;
                            }
                            return false;
                        }

                    case Builtin.SumUpdateAndRepeat:
                        {
                            var lhs = frameBase + code[pc++];
                            var rhs = Deref(frameBase + code[pc++]);
                            DataStack[lhs].floatingPoint += DataStack[rhs].AsFloat;
                            return false;
                        }

                    case Builtin.IncAndRepeat:
                        {
                            var arg = frameBase + code[pc++];
                            DataStack[arg].integer++;
                            return false;
                        }

                    case Builtin.LessThan:
                    {
                        pc = FunctionalExpression.Eval(predicate, code, pc, goalFrame, frameBase, dTop);
                        pc = FunctionalExpression.Eval(predicate, code, pc, goalFrame, frameBase, (ushort)(dTop+1));
                        var leftArg = DataStack[dTop + FunctionalExpression.EvalStackOffset].AsFloat;
                        var rightArg = DataStack[dTop + 1 + FunctionalExpression.EvalStackOffset].AsFloat;
                        return leftArg < rightArg;
                    }

                    case Builtin.LessEq:
                        {
                            pc = FunctionalExpression.Eval(predicate, code, pc, goalFrame, frameBase, dTop);
                            pc = FunctionalExpression.Eval(predicate, code, pc, goalFrame, frameBase, (ushort)(dTop + 1));
                            var leftArg = DataStack[dTop + FunctionalExpression.EvalStackOffset].AsFloat;
                            var rightArg = DataStack[dTop + 1 + FunctionalExpression.EvalStackOffset].AsFloat;
                            return leftArg <= rightArg;
                        }

                    case Builtin.GreaterThan:
                        {
                            pc = FunctionalExpression.Eval(predicate, code, pc, goalFrame, frameBase, dTop);
                            pc = FunctionalExpression.Eval(predicate, code, pc, goalFrame, frameBase, (ushort)(dTop + 1));
                            var leftArg = DataStack[dTop + FunctionalExpression.EvalStackOffset].AsFloat;
                            var rightArg = DataStack[dTop + 1 + FunctionalExpression.EvalStackOffset].AsFloat;
                            return leftArg > rightArg;
                        }

                    case Builtin.GreaterEq:
                        {
                            pc = FunctionalExpression.Eval(predicate, code, pc, goalFrame, frameBase, dTop);
                            pc = FunctionalExpression.Eval(predicate, code, pc, goalFrame, frameBase, (ushort)(dTop + 1));
                            var leftArg = DataStack[dTop + FunctionalExpression.EvalStackOffset].AsFloat;
                            var rightArg = DataStack[dTop + 1 + FunctionalExpression.EvalStackOffset].AsFloat;
                            return leftArg >= rightArg;
                        }

                    case Builtin.IntegerTest:
                        {
                            pc = FunctionalExpression.Eval(predicate, code, pc, goalFrame, frameBase, dTop);
                            return DataStack[dTop + FunctionalExpression.EvalStackOffset].Type == TaggedValueType.Integer;
                        }

                    case Builtin.FloatTest:
                        {
                            pc = FunctionalExpression.Eval(predicate, code, pc, goalFrame, frameBase, dTop);
                            return DataStack[dTop + FunctionalExpression.EvalStackOffset].Type == TaggedValueType.Float;
                        }

                    case Builtin.NumberTest:
                        {
                            pc = FunctionalExpression.Eval(predicate, code, pc, goalFrame, frameBase, dTop);
                            var argType = DataStack[dTop + FunctionalExpression.EvalStackOffset].Type;
                            return argType == TaggedValueType.Integer || argType == TaggedValueType.Float;
                        }

                    case Builtin.StringTest:
                        {
                            pc = FunctionalExpression.Eval(predicate, code, pc, goalFrame, frameBase, dTop);
                            var addr = dTop + FunctionalExpression.EvalStackOffset;
                            return DataStack[addr].Type == TaggedValueType.Reference
                                   && DataStack[addr].reference is string;
                        }

                    case Builtin.SymbolTest:
                        {
                            pc = FunctionalExpression.Eval(predicate, code, pc, goalFrame, frameBase, dTop);
                            var addr = dTop + FunctionalExpression.EvalStackOffset;
                            return DataStack[addr].Type == TaggedValueType.Reference
                                   && DataStack[addr].reference is Symbol;
                        }

                    case Builtin.MissingTest:
                        {
                            pc = FunctionalExpression.Eval(predicate, code, pc, goalFrame, frameBase, dTop);
                            var addr = dTop + FunctionalExpression.EvalStackOffset;
                            return DataStack[addr].Type == TaggedValueType.Reference
                                   && DataStack[addr].reference is Missing;
                        }

                    case Builtin.TestNotFalse:
                        {
                            pc = FunctionalExpression.Eval(predicate, code, pc, goalFrame, frameBase, dTop);
                            var addr = dTop + FunctionalExpression.EvalStackOffset;
                            // Accept anything but the constant false
                            return DataStack[addr].Type != TaggedValueType.Boolean
                                   || DataStack[addr].boolean;
                        }

                    case Builtin.Throw:
                    {
                        pc = FunctionalExpression.Eval(predicate, code, pc, goalFrame, frameBase, dTop);
                        var addr = dTop + FunctionalExpression.EvalStackOffset;
                        // Accept anything but the constant false
                        var arg = DataStack[addr].Value;
                        if (DataStack[addr].Type == TaggedValueType.Reference && arg is Exception e)
                            throw e;
                        throw new ArgumentTypeException("throw", 0, "Argument should be an exception", arg);
                    }

                    case Builtin.CallFailed:
                    {
                        var p = predicate.GetObjectConstant<Symbol>(code[pc++]);
                        throw new CallFailedException(p);
                    }

                    default:
                        throw new InvalidOperationException("Unknown builtin opcode: "+goalCode[goalPc-1]);
                }
            }
            #endregion

            try
            {
                while (true)
                {
                    Profiler.MaybeSampleStack(goalFrame);
                    SanityCheckStack(goalFrame, eTop, cTop, dTop);
                    //Debug.Assert(
                    //    (goal == Trampoline && startOfCall == 0) || goal[startOfCall - 2] == (byte) Opcode.CGoal,
                    //    "Invalid startOfCall value");
                    var headInstruction = (Opcode) headCode[headPc++];
                    var goalInstruction = (Opcode) goalCode[goalPc++];

#if DEBUG
                    if (
                        !CheckDebug(goalFrame, headInstruction, goalInstruction, headPredicate, headCode, headPc, eTop, cTop,
                            dTop)) return false;
#endif
                    if (goalInstruction >= Opcode.CCall && headPredicate.IsTraced)
                    {
                        TraceCall(headPredicate, dTop, headCode, headPc, headRule);
                    }

                    switch ((int) headInstruction + (int) goalInstruction)
                    {
                            #region Argument matching instructions

                        //
                        // Constant/Constant matching
                        //
                        case (int) Opcode.HeadConst + (int) Opcode.GoalConst:
                            var headCType = (OpcodeConstantType) headCode[headPc++];
                            var goalCType = (OpcodeConstantType) goalCode[goalPc++];
                            if (headCType == goalCType)
                            {
                                switch (headCType)
                                {
                                    case OpcodeConstantType.Boolean:
                                    case OpcodeConstantType.SmallInteger:
                                        if (headCode[headPc++] != goalCode[goalPc++])
                                            goto fail;
                                        break;

                                    case OpcodeConstantType.Integer:
                                        if (headPredicate.GetIntConstant(headCode[headPc++])
                                            != goalPredicate.GetIntConstant(goalCode[goalPc++]))
                                            goto fail;
                                        break;

                                    case OpcodeConstantType.Float:
                                        // ReSharper disable once CompareOfFloatsByEqualityOperator
                                        if (headPredicate.GetFloatConstant(headCode[headPc++])
                                            != goalPredicate.GetFloatConstant(goalCode[goalPc++]))
                                            goto fail;
                                        break;

                                    case OpcodeConstantType.Object:
                                        var headValue = headPredicate.GetObjectConstant<object>(headCode[headPc++]);
                                        var goalValue = goalPredicate.GetObjectConstant<object>(goalCode[goalPc++]);
                                        if (!Equals(headValue, goalValue))
                                            goto fail;
                                        break;

                                    case OpcodeConstantType.FunctionalExpression:
                                    {
                                        // Get the value of the functional expression
                                        headPc = FunctionalExpression.Eval(headPredicate, headCode, headPc, goalFrame, dTop, dTop);
                                        var resultAddress = dTop + FunctionalExpression.EvalStackOffset;
                                        goalPc = FunctionalExpression.Eval(goalPredicate, goalCode, goalPc, goalFrame,
                                            EnvironmentStack[goalFrame].Base,
                                            (ushort) (dTop + 1));
                                        // Goal result is not ad address resultAddress+1
                                        var headResultType = DataStack[resultAddress].Type;
                                        if (headResultType != DataStack[resultAddress + 1].Type)
                                            goto fail;
                                        switch (headResultType)
                                        {
                                            case TaggedValueType.Boolean:
                                                if (DataStack[resultAddress].boolean !=
                                                    DataStack[resultAddress + 1].boolean)
                                                    goto fail;
                                                break;
                                            case TaggedValueType.Integer:
                                                if (DataStack[resultAddress].integer !=
                                                    DataStack[resultAddress + 1].integer)
                                                    goto fail;
                                                break;
                                            case TaggedValueType.Float:
                                                // ReSharper disable once CompareOfFloatsByEqualityOperator
                                                if (DataStack[resultAddress].floatingPoint !=
                                                    DataStack[resultAddress + 1].floatingPoint)
                                                    goto fail;
                                                break;
                                            case TaggedValueType.Reference:
                                                if (!Equals(DataStack[resultAddress].reference,
                                                            DataStack[resultAddress + 1].reference))
                                                    goto fail;
                                                break;
                                        }
                                    }
                                        break;
                                }
                            }
                            else if (headCType == OpcodeConstantType.FunctionalExpression)
                            {
                                // Get the value of the functional expression
                                headPc = FunctionalExpression.Eval(headPredicate, headCode, headPc, goalFrame, dTop, dTop);
                                var resultAddress = dTop + FunctionalExpression.EvalStackOffset;
                                switch (goalCType)
                                {
                                    case OpcodeConstantType.Boolean:
                                        if (!DataStack[resultAddress].Equal(goalCode[goalPc++] != 0))
                                            goto fail;
                                        break;

                                    case OpcodeConstantType.SmallInteger:
                                        if (!DataStack[resultAddress].Equal((sbyte) goalCode[goalPc++]))
                                            goto fail;
                                        break;

                                    case OpcodeConstantType.Integer:
                                        if (
                                            !DataStack[resultAddress].Equal(goalPredicate.GetIntConstant(goalCode[goalPc++])))
                                            goto fail;
                                        break;
                                    case OpcodeConstantType.Float:
                                        if (
                                            !DataStack[resultAddress].Equal(
                                                goalPredicate.GetFloatConstant(goalCode[goalPc++])))
                                            goto fail;
                                        break;
                                    case OpcodeConstantType.Object:
                                        if (
                                            !DataStack[resultAddress].EqualReference(
                                                goalPredicate.GetObjectConstant<object>(goalCode[goalPc++])))
                                            goto fail;
                                        break;
                                }
                            }
                            else if (goalCType == OpcodeConstantType.FunctionalExpression)
                            {
                                goalPc = FunctionalExpression.Eval(goalPredicate, goalCode, goalPc, goalFrame,
                                    EnvironmentStack[goalFrame].Base, dTop);
                                var resultAddress = dTop + FunctionalExpression.EvalStackOffset;
                                switch (headCType)
                                {
                                    case OpcodeConstantType.Boolean:
                                        if (!DataStack[resultAddress].Equal(headCode[headPc++] != 0))
                                            goto fail;
                                        break;

                                    case OpcodeConstantType.SmallInteger:
                                        if (!DataStack[resultAddress].Equal((sbyte) headCode[headPc++]))
                                            goto fail;
                                        break;

                                    case OpcodeConstantType.Integer:
                                        if (
                                            !DataStack[resultAddress].Equal(headPredicate.GetIntConstant(headCode[headPc++])))
                                            goto fail;
                                        break;
                                    case OpcodeConstantType.Float:
                                        if (
                                            !DataStack[resultAddress].Equal(
                                                headPredicate.GetFloatConstant(headCode[headPc++])))
                                            goto fail;
                                        break;
                                    case OpcodeConstantType.Object:
                                        if (
                                            !DataStack[resultAddress].EqualReference(
                                                goalPredicate.GetObjectConstant<object>(goalCode[goalPc++])))
                                            goto fail;
                                        break;
                                }
                            }
                            else if (IsNumericCType(headCType) && IsNumericCType(goalCType))
                            {
                                // Mixed float match
                                // ReSharper disable once CompareOfFloatsByEqualityOperator
                                if (headPredicate.GetFloatConstant(headCType, headCode[headPc++])
                                    != goalPredicate.GetFloatConstant(goalCType, goalCode[goalPc++]))
                                    goto fail;
                            }
                            else
                                goto fail;
                            break;

                        //
                        // Void/X, X/Void matching
                        //
                        case (int) Opcode.HeadVoid + (int) Opcode.GoalVoid:
                            // Nothing to do
                            break;

                        case (int) Opcode.HeadVoid + (int) Opcode.GoalVarMatch:
                            // Nothing to do
                            goalPc++;
                            break;

                        case (int) Opcode.HeadVarMatch + (int) Opcode.GoalVoid:
                            // Nothing to do
                            headPc++;
                            break;

                        case (int) Opcode.HeadVoid + (int) Opcode.GoalConst:
                            // Don't bother reading the constant
                            if ((OpcodeConstantType) goalCode[goalPc++] == OpcodeConstantType.FunctionalExpression)
                            {
                                // skip to the end of the expression
                                // ReSharper disable once EmptyEmbeddedStatement
                                while ((FOpcode) goalCode[goalPc++] != FOpcode.Return) ;
                            }
                            else
                                goalPc++;
                            break;

                        case (int) Opcode.HeadConst + (int) Opcode.GoalVoid:
                            // Don't bother reading the constant
                            if ((OpcodeConstantType) headCode[headPc++] == OpcodeConstantType.FunctionalExpression)
                            {
                                // skip to the end of the expression
                                while ((FOpcode) headCode[headPc++] != FOpcode.Return)
                                {
                                }
                            }
                            else
                                headPc++;
                            break;

                        case (int) Opcode.HeadVoid + (int) Opcode.GoalVarFirst:
                            // First reference to goal variable is a void variable, so we just initialize
                            // the goal variable to be unbound.
                            Unbind(EnvironmentStack[goalFrame].Base + goalCode[goalPc++]);
                            break;

                        case (int) Opcode.HeadVarFirst + (int) Opcode.GoalVoid:
                            // First reference to head variable is a void variable, so we just initialize
                            // the head variable to be unbound.
                            Unbind(dTop + headCode[headPc++]);
                            break;

                        //
                        // Var/Const matching
                        //
                        case (int) Opcode.HeadConst + (int) Opcode.GoalVarFirst:
                            headPc = SetVarToConstant(EnvironmentStack[goalFrame].Base + goalCode[goalPc++],
                                headPredicate, headCode, headPc, goalFrame,
                                dTop,
                                dTop);
                            break;

                        case (int) Opcode.HeadVarFirst + (int) Opcode.GoalConst:
                            goalPc = SetVarToConstant(dTop + headCode[headPc++],
                                goalPredicate, goalCode, goalPc, goalFrame,
                                EnvironmentStack[goalFrame].Base,
                                dTop);
                            break;

                        case (int) Opcode.HeadConst + (int) Opcode.GoalVarMatch:
                            if (!MatchVarConstant(EnvironmentStack[goalFrame].Base + goalCode[goalPc++],
                                headPredicate, headCode, ref headPc, goalFrame,
                                dTop,
                                dTop))
                                goto fail;
                            break;

                        case (int) Opcode.HeadVarMatch + (int) Opcode.GoalConst:
                            if (!MatchVarConstant(dTop + headCode[headPc++],
                                goalPredicate, goalCode, ref goalPc, goalFrame,
                                EnvironmentStack[goalFrame].Base,
                                dTop))
                                goto fail;
                            break;

                        //
                        // Var/Var matching
                        //
                        case (int) Opcode.HeadVarFirst + (int) Opcode.GoalVarFirst:
                        {
                            var goalVarAddress = EnvironmentStack[goalFrame].Base + goalCode[goalPc++];
                            Unbind(goalVarAddress);
                            DataStack[dTop + headCode[headPc++]].AliasTo(goalVarAddress);
                        }
                            break;

                        case (int) Opcode.HeadVarFirst + (int) Opcode.GoalVarMatch:
                        {
                            var headAddress = (ushort) (dTop + headCode[headPc++]);
                            var goalAddress = Deref(EnvironmentStack[goalFrame].Base + goalCode[goalPc++]);
                            Debug.Assert(headAddress != goalAddress, "Aliasing variable to itself");
                            DataStack[headAddress].AliasTo(goalAddress);
                        }
                            break;

                        case (int) Opcode.HeadVarMatch + (int) Opcode.GoalVarFirst:
                        {
                            var goalVarAddress = (ushort) (EnvironmentStack[goalFrame].Base + goalCode[goalPc++]);
                            Unbind(goalVarAddress);
                            var headVarAddress = dTop + headCode[headPc++];
                            UnifyDereferenced(goalVarAddress, Deref(headVarAddress));
                        }
                            break;

                        case (int) Opcode.HeadVarMatch + (int) Opcode.GoalVarMatch:
                        {
                            var goalVarAddress = (ushort) (EnvironmentStack[goalFrame].Base + goalCode[goalPc++]);
                            var headVarAddress = dTop + headCode[headPc++];
                            if (!UnifyDereferenced(Deref(goalVarAddress), Deref(headVarAddress)))
                                goto fail;
                        }
                            break;

                            #endregion

                            #region Call instructions

                        //
                        // Call instructions
                        //

                        case (int) Opcode.CCut + (int) Opcode.CLastCall:
                        case (int) Opcode.CCut + (int) Opcode.CCall:
                            cTop = callerCp;
                            goalPc--;
                            break;

                        case (int) Opcode.CBuiltin + (int) Opcode.CLastCall:
                        case (int) Opcode.CBuiltin + (int) Opcode.CCall:
                        {
                            var headBase = headPredicate.IsNestedPredicate ? EnvironmentStack[goalFrame].Base : dTop;
                            if (!DoBuiltin(headPredicate, headCode, ref headPc, headBase)) goto fail;
                            goalPc--;
                        }
                            break;

                        // Tail call a special predicate
                        case (int) Opcode.CSpecial + (int) Opcode.CLastCall:
                            if (headPredicate.Table != null)
                            {
                                Profiler.MaybeSampleStack(goalFrame, headPredicate);
                                bool canContinue;
                                var nextRow = headPredicate.Table.MatchTableRows(restartedClauseNumber, dTop,
                                    out canContinue);
                                restartedClauseNumber = 0;
                                if (nextRow == 0)
                                    goto fail;
                                if (canContinue)
                                {
                                    ChoicePointStack[cTop++] = new ChoicePoint(goalFrame, startOfCall, headPredicate,
                                        nextRow, dTopSave, trailSave, UTop, eTop);
                                }
                            }
                            else
                            {
                                var savedUTop = UTop;
                                switch (headPredicate.PrimopImplementation(dTop, restartedClauseNumber))
                                {
                                    case CallStatus.Fail:
                                        goto fail;

                                    case CallStatus.DeterministicSuccess:
                                        break;

                                    case CallStatus.NonDeterministicSuccess:
                                        // Reserve space for temp vars
                                        if (restartedClauseNumber == 0)
                                            dTop += headPredicate.Tempvars;
                                        ChoicePointStack[cTop++] = new ChoicePoint(goalFrame, startOfCall,
                                            headPredicate, (ushort) (restartedClauseNumber + 1),
                                            dTopSave, trailSave, savedUTop, eTop);
                                        break;

                                    case CallStatus.CallIndirect:
                                        throw new InvalidOperationException("Tail call to primop that returned callindirect.");
                                }
                                restartedClauseNumber = 0;
                            }
                            goto goalPredicateSucceeded;

                        // Tail calling a fact
                        case (int) Opcode.CNoGoal + (int) Opcode.CLastCall:
                            goalPredicateSucceeded:
                            if (goalPredicate.IsTraced)
                            {
                                TraceSucceed(dTop, headPredicate, headCode);
                            }
                            // Succeed
                            // Walk up stack until we find something that's incomplete
                            do
                            {
                                if (goalFrame == 0)
                                    return true;

                                goalPc = EnvironmentStack[goalFrame].ContinuationPc;
                                goalFrame = EnvironmentStack[goalFrame].ContinuationFrame;
                                goalPredicate = EnvironmentStack[goalFrame].Predicate;
                                //goal = EnvironmentStack[goalFrame].Clause;
                                goalCode = EnvironmentStack[goalFrame].CompiledClause.Code;
                                if (goalPredicate.IsTraced && goalPc == goalCode.Length)
                                {
                                    TraceSucceed(EnvironmentStack[goalFrame].Base, goalPredicate, goalCode);
                                }
                            } while (goalPc == goalCode.Length);

                            continuationLoop:
#if DEBUG
                            if (goalPc == goalCode.Length)
                            {
                                StandardError.WriteLine("Invalid code segment in {0}", goalPredicate);
                                foreach (var b in goalCode)
                                    StandardError.Write("{0} ", b);
                                StandardError.WriteLine();
                            }
#endif
                            switch ((Opcode) goalCode[goalPc++])
                            {
                                case Opcode.CGoal:
                                    break;

                                case Opcode.CBuiltin:
                                    if (!DoBuiltin(goalPredicate, goalCode, ref goalPc, EnvironmentStack[goalFrame].Base))
                                        goto fail;
                                    goto continuationLoop;

                                case Opcode.CCut:
                                    cTop = EnvironmentStack[goalFrame].CallerCTop;
                                    goto continuationLoop;

                                case Opcode.CNoGoal:
                                    goto goalPredicateSucceeded;

                                default:
                                    throw new InvalidOperationException($"Invalid opcode {goalCode[goalPc-1]}/{(Opcode)goalCode[goalPc - 1]}");
                            }

                            Debug.Assert(goalCode[goalPc - 1] == (byte) Opcode.CGoal);
                            headPredicate = goalPredicate.GetObjectConstant<Predicate>(goalCode[goalPc++]);
                          beginCall:
                            headPc = 0;
                            startOfCall = goalPc;
                            //Debug.Assert(goal[startOfCall - 2] == (byte) Opcode.CGoal, "Invalid startOfCall");
                            trailSave = TrailTop;
                            dTopSave = dTop;
                            if (headPredicate.FirstClause == null)
                            {
                                if (headPredicate.IsLocked)
                                    goto fail;
                                throw new Exception("Undefined predicate: " + headPredicate);
                            }
                            headRule = headPredicate.FirstClause;
                            headCode = headRule.Code;
                            callerCp = cTop;

                            // Allocate new choice frame, if necessary
                            if (headPredicate.ExtraClauses != null)
                                ChoicePointStack[cTop++] = new ChoicePoint(goalFrame, startOfCall,
                                    headPredicate, 0,
                                    dTopSave, trailSave, UTop, eTop);
                            break;

                        // (Non-tail) calling a special predicate
                        case (int) Opcode.CSpecial + (int) Opcode.CCall:
                            if (headPredicate.Table != null)
                            {
                                Profiler.MaybeSampleStack(goalFrame, headPredicate);
                                bool canContinue;
                                var nextRow = headPredicate.Table.MatchTableRows(restartedClauseNumber, dTop,
                                    out canContinue);
                                restartedClauseNumber = 0;
                                if (nextRow == 0)
                                    goto fail;
                                if (canContinue)
                                    ChoicePointStack[cTop++] = new ChoicePoint(goalFrame, startOfCall,
                                        headPredicate, nextRow,
                                        dTopSave, trailSave, UTop, eTop);
                            }
                            else
                            {
                                var savedUTop = UTop;
                                switch (headPredicate.PrimopImplementation(dTop, restartedClauseNumber))
                                {
                                    case CallStatus.Fail:
                                        goto fail;

                                    case CallStatus.DeterministicSuccess:
                                        break;

                                    case CallStatus.NonDeterministicSuccess:
                                        // Reserve space for temp vars
                                        if (restartedClauseNumber == 0)
                                            dTop += headPredicate.Tempvars;
                                        ChoicePointStack[cTop++] = new ChoicePoint(goalFrame, startOfCall,
                                            headPredicate, (ushort) (restartedClauseNumber + 1),
                                            dTopSave, trailSave, savedUTop, eTop);
                                        break;

                                    case CallStatus.CallIndirect:
                                        headPredicate = (Predicate)DataStack[dTop].reference;
                                        // Goal code falls through to argument instructions w/o an intervening CGoal instruction.
                                        goto beginCall;
                                }
                                restartedClauseNumber = 0;
                            }
                            goto continueGoalPredicate;

                        // (Non-tail) calling a fact
                        case (int) Opcode.CNoGoal + (int) Opcode.CCall:
                            // We're done; move on to the next call in goal
                            if (headPredicate.IsTraced)
                            {
                                TraceSucceed(dTop, headPredicate, headCode);
                            }

                            continueGoalPredicate:
                            switch ((Opcode) goalCode[goalPc++])
                            {
                                case Opcode.CGoal:
                                    headPc = 0;
                                    // ReSharper disable once PossibleNullReferenceException
                                    headPredicate = goalPredicate.GetObjectConstant<Predicate>(goalCode[goalPc++]);
                                    startOfCall = goalPc;
                                    Debug.Assert(goalCode[startOfCall - 2] == (byte) Opcode.CGoal, "Invalid startOfCall");
                                    trailSave = TrailTop;
                                    dTopSave = dTop;
                                    if (headPredicate.FirstClause == null)
                                    {
                                        if (headPredicate.IsLocked)
                                            goto fail;
                                        throw new Exception("Undefined predicate: " + headPredicate);
                                    }
                                    headRule = headPredicate.FirstClause;
                                    headCode = headRule.Code;
                                    callerCp = cTop;
                                    // Allocate new choice frame, if necessary
                                    if (headPredicate.ExtraClauses != null)
                                        ChoicePointStack[cTop++] = new ChoicePoint(goalFrame, startOfCall,
                                            headPredicate, 0,
                                            dTopSave, trailSave, UTop, eTop);
                                    break;

                                case Opcode.CCut:
                                    cTop = EnvironmentStack[goalFrame].CallerCTop;
                                    if (goalPc == goalCode.Length)
                                        // At end of goalPredicate, so return from it.
                                        goto goalPredicateSucceeded;
                                    goto continueGoalPredicate;

                                case Opcode.CBuiltin:
                                    if (!DoBuiltin(goalPredicate, goalCode, ref goalPc, EnvironmentStack[goalFrame].Base))
                                        goto fail;
                                    goto continueGoalPredicate;

                                case Opcode.CNoGoal:
                                    goto goalPredicateSucceeded;

                                default:
                                    Debug.Assert(false, "CCall should be followed by CGoal");
                                    break;
                            }
                            break;

                        // Tail calling a rule
                        case (int) Opcode.CGoal + (int) Opcode.CLastCall:
                            if ((cTop > 0 && ChoicePointStack[cTop - 1].CallingFrame >= goalFrame)
                                || headPredicate.ExtraClauses != null)
                                // The current goal frame has a choicepoint so we can't tail call
                                goto nonTailGoalCall;
                            EnvironmentStack[goalFrame].Predicate = goalPredicate = headPredicate;
                            EnvironmentStack[goalFrame].CompiledClause = headRule;
                            goalCode = headCode;
                            EnvironmentStack[goalFrame].CallerCTop = callerCp;

                            if (!goalPredicate.IsNestedPredicate)
                            {
                                // Allocate space for args
                                EnvironmentStack[goalFrame].Base = dTop;
                                dTop += headRule.EnvironmentSize;
                            } // else base for nested predicate is just the base for the caller.

                            goalPc = headPc;
                            headPredicate = goalPredicate.GetObjectConstant<Predicate>(goalCode[goalPc++]);
                            startOfCall = goalPc;
                            Debug.Assert(goalCode[startOfCall - 2] == (byte) Opcode.CGoal, "Invalid startOfCall");
                            trailSave = TrailTop;
                            dTopSave = dTop;

                            if (headPredicate.FirstClause == null)
                            {
                                if (headPredicate.IsLocked)
                                    goto fail;
                                throw new Exception("Undefined predicate: " + headPredicate);
                            }
                            headRule = headPredicate.FirstClause;
                            headCode = headRule.Code;
                            callerCp = cTop;

                            // Allocate new choice frame, if necessary
                            if (headPredicate.ExtraClauses != null)
                                ChoicePointStack[cTop++] = new ChoicePoint(goalFrame, startOfCall,
                                    headPredicate, 0,
                                    dTopSave, trailSave, UTop, eTop);

                            headPc = 0;
                            break;

                        // Non-tail calling a rule
                        case (int) Opcode.CGoal + (int) Opcode.CCall:
                            nonTailGoalCall:
                            // Allocate new stack frame for clause we're about to enter
                            var newFrame = eTop++;
                            if (headPredicate.IsNestedPredicate)
                            {
                                EnvironmentStack[newFrame] =
                                    new Environment(headPredicate, headRule,
                                        // Our vars are really our caller's vars.
                                        EnvironmentStack[goalFrame].Base,
                                        goalFrame, callerCp, goalPc);
                            }
                            else
                            {
                                EnvironmentStack[newFrame] =
                                    new Environment(headPredicate, headRule,
                                        // Allocate DStack space
                                        dTop,
                                        goalFrame, callerCp, goalPc);
                                dTop += headRule.EnvironmentSize;
                            }

                            // Jump into clause
                            goalPredicate = headPredicate;
                            goalCode = headCode;
                            goalFrame = newFrame;
                            goalPc = headPc;

                            // We know we just fetched a CGoal, so get the predicate being called
                            headPredicate = goalPredicate.GetObjectConstant<Predicate>(goalCode[goalPc++]);
                            startOfCall = goalPc;
                            Debug.Assert(goalCode[startOfCall - 2] == (byte) Opcode.CGoal, "Invalid startOfCall");
                            trailSave = TrailTop;
                            dTopSave = dTop;
                            // Fail if it has no rules
                            if (headPredicate.FirstClause == null)
                                goto fail;
                            // Otherwise start matching the head
                            headRule = headPredicate.FirstClause;
                            headCode = headRule.Code;
                            headPc = 0;
                            callerCp = cTop;

                            // Allocate new choice frame, if necessary
                            if (headPredicate.ExtraClauses != null && headCode == headPredicate.FirstClause.Code)
                                ChoicePointStack[cTop++] = new ChoicePoint(goalFrame, startOfCall,
                                    headPredicate, 0, dTopSave,
                                    trailSave, UTop, eTop);
                            break;

#endregion


                        default:
                            Debug.Assert(false,
                                $"Unknown opcode combination: head={headInstruction}, goal={goalInstruction}");

#region Fail handling

                            //
                            // FAIL
                            //
                            fail:
#if DEBUG
                            DebugConsoleFailMessage(headPredicate, goalFrame, eTop, cTop);
#endif
                            if (headPredicate.IsTraced || goalPredicate.IsTraced)
                                StandardError.WriteLine($"*FAIL*: Call from {goalPredicate} to {headPredicate}");

                            if (cTop == 0)
                                // No choice points
                                return false;

                            // Restart at last choicepoint
                            var cp = ChoicePointStack[--cTop];
                            goalFrame = cp.CallingFrame;
                            eTop = cp.SavedETop;
                            dTopSave = dTop = cp.DataStackTop;
                            UndoTo(cp.TrailTop, cp.UndoStackTop);
                            trailSave = cp.TrailTop;
                            goalPredicate = EnvironmentStack[goalFrame].Predicate;
                            goalCode = EnvironmentStack[goalFrame].CompiledClause.Code;
                            startOfCall = goalPc = cp.CallingPC;
                            callerCp = cTop;
                            //Debug.Assert(goalFrame == 0 || goal[startOfCall - 2] == (byte) Opcode.CGoal,
                            //    "Invalid startOfCall");

#if DEBUG
                            if (SingleStep)
                                StandardError.WriteLine($"Restored to dTop={dTop}, TrailTop={TrailTop}");
#endif

                            headPredicate = cp.Callee;
                            //Trace.WriteLine($"Restarting {headPredicate}");

                            if (headPredicate.IsSpecial)
                            {
                                // Restarting a table lookup
                                restartedClauseNumber = cp.NextClause;
                                headRule = headPredicate.FirstClause;
                            }
                            else
                            {
                                // Restarting a normal choicepoint.
                                restartedClauseNumber = 0;
                                headRule = headPredicate.ExtraClauses[cp.NextClause];
                                // Allocate new choice frame, if necessary
                                if (headPredicate.ExtraClauses.Count > cp.NextClause + 1)
                                {
                                    ChoicePointStack[cTop++].NextClause += 1;
                                }
                            }
                            headCode = headRule.Code;

                            DebugConsoleRestartMessage(goalPredicate, headPredicate, cp);

                            headPc = 0;

#endregion

                            break;
                    }
                }
            }
            catch (Exception e)
            {
                if (StandardError != null)
                {
                    StandardError.WriteLine($"\n\n{e.GetType().Name}: {e.Message}");
                    DumpStackWithHead(goalFrame, eTop, cTop, dTop, headPredicate, headCode, headPc);
                }
                throw;
            }
        }

        /// <summary>
        /// Generate a trace report for a predicate that succeeds.
        /// </summary>
        private static void TraceSucceed(ushort dTopSave, Predicate goalPredicate, byte[] goal)
        {
            StandardError.Write("Succeed: ");
            DumpHead(dTopSave, goalPredicate, goal, 9999);
        }

        /// <summary>
        /// Generate a trace report for a call to a predicate
        /// </summary>
        private static void TraceCall(Predicate headPredicate, ushort dTop, byte[] head, ushort headPc, CompiledClause headRule)
        {
            StandardError.Write("Enter: ");
            DumpHead(dTop, headPredicate, head, headPc);
            StandardError.Write("   ");
            StandardError.WriteLine(headRule.Source);
        }

        [Conditional("DEBUG")]
        // ReSharper disable once UnusedParameter.Local
        private static void SanityCheckStack(ushort goalFrame, ushort eTop, ushort cTop,
            // ReSharper disable once UnusedParameter.Local
            ushort dTop)
        {
            Debug.Assert(goalFrame < eTop, "Goal frame points to unallocated environment frame")
                ;
            for (var e = 0; e < eTop; e++)
            {
                Debug.Assert(EnvironmentStack[e].ContinuationFrame < eTop,
                    "Environment frame has continuation to unallocated frame.");
                Debug.Assert(e==0 || EnvironmentStack[e].CallerCTop <= cTop,
                    "Environment frame has invalid saved cTop.");
                Debug.Assert(EnvironmentStack[e].Base <= dTop,
                    "Base address for environment frame points to unallocated space");
            }

            for (var c = 0; c < cTop; c++)
            {
                var callingFrame = ChoicePointStack[c].CallingFrame;
                Debug.Assert(callingFrame < eTop, 
                    "Choicepoint has unallocated calling frame.");
                // ReSharper disable once RedundantAssignment
                var callCTop = EnvironmentStack[callingFrame].CallerCTop;
                Debug.Assert(callingFrame == 0 || callCTop == 0 || callCTop-1 < c,
                    "Backtracks to environment frame with same or deeper choicepoint.");
                Debug.Assert(ChoicePointStack[c].DataStackTop <= dTop,
                    "Saved dTop for choicepoint points to unallocated space.");
            }
        }
#endregion

#region Trailing and Undo Stack
        /// <summary>
        /// Add variable to trail.  Called when a variable is bound to a value so the system knows
        /// to unbind it upon backtracking.
        /// </summary>
        /// <param name="address">Address of the variable in the DataStack</param>
        internal static void SaveVariable(ushort address)
        {
            Trail[TrailTop++] = address;
        }
        
        /// <summary>
        /// Undo variable bindings and run pending undo operations.
        /// </summary>
        /// <param name="cpTrailTop">Trail position to undo back to</param>
        /// <param name="uStackTop">Undo stack position to undo back to.</param>
        private static void UndoTo(ushort cpTrailTop, ushort uStackTop)
        {
            ushort t;
            UndoTo(cpTrailTop);
            t = UTop;
            while (t>uStackTop)
                UndoStack[--t].Invoke();
            UTop = t;
        }

        /// <summary>
        /// Undo variable bindings but don't run undo stack operations.
        /// </summary>
        /// <param name="cpTrailTop">Trail position to reset to.</param>
        public static void UndoTo(ushort cpTrailTop)
        {
            var t = TrailTop;
            while (t > cpTrailTop)
                DataStack[Trail[--t]].Type = TaggedValueType.Unbound;
            TrailTop = cpTrailTop;
        }
#endregion

#region Unification
        private static bool UnifyDereferenced(ushort a1, ushort a2)
        {
            if (DataStack[a1].Type == TaggedValueType.Unbound)
            {
                if (DataStack[a2].Type == TaggedValueType.Unbound)
                    AliasDereferenced(a1, a2);
                else
                    SetVariableToDereferencedBoundVariableValue(a1, a2);
            }
            else if (DataStack[a2].Type == TaggedValueType.Unbound)
                SetVariableToDereferencedBoundVariableValue(a2, a1);
            else
                return BoundVariablesAreEqual(a1, a2);
            return true;
        }

        private static bool BoundVariablesAreEqual(ushort a1, ushort a2)
        {
            if (DataStack[a1].Type != DataStack[a2].Type)
                return false;
            switch (DataStack[a1].Type)
            {
                case TaggedValueType.Boolean:
                    return DataStack[a1].boolean == DataStack[a2].boolean;

                case TaggedValueType.Integer:
                    return DataStack[a1].integer == DataStack[a2].integer;

                case TaggedValueType.Float:
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    return DataStack[a1].floatingPoint == DataStack[a2].floatingPoint;

                case TaggedValueType.Reference:
                    return DataStack[a1].reference == DataStack[a2].reference;

                default:
                    throw new InvalidOperationException("Invalid tagged value type "+DataStack[a1].Type);
            }
        }
#endregion

#region Variable manipulation
        private static void SetVariableToDereferencedBoundVariableValue(ushort addressToSet, ushort addressToRead)
        {
            Debug.Assert(DataStack[addressToRead].Type != TaggedValueType.VariableForward, "Setting variable to underefed address");
            SaveVariable(addressToSet);
            DataStack[addressToSet] = DataStack[addressToRead];
        }

        private static void AliasDereferenced(ushort a1, ushort a2)
        {
            if (a1 == a2)
                return;
            if (a1 > a2)
            {
                SaveVariable(a1);
                DataStack[a1].AliasTo(a2);
            }
            else
            {
                SaveVariable(a2);
                DataStack[a2].AliasTo(a1);
            }
        }

        private static bool MatchVarConstant(int address, Predicate p, byte[] clause, ref ushort pc, ushort goalFrame, ushort frameBase, ushort dTop)
        {
            var a = Deref((ushort)address);
            if (DataStack[a].Type == TaggedValueType.Unbound)
            {
                pc = SetVarToConstant(a, p, clause, pc, goalFrame, frameBase, dTop);
                return true;
            }

            switch ((OpcodeConstantType) clause[pc++])
            {
                case OpcodeConstantType.Boolean:
                    return DataStack[a].Equal(clause[pc++] != 0);

                case OpcodeConstantType.SmallInteger:
                    var val = (sbyte) clause[pc++];
                    return DataStack[a].Equal(val);

                case OpcodeConstantType.Integer:
                    return DataStack[a].Equal(p.GetIntConstant(clause[pc++]));

                case OpcodeConstantType.Float:
                    return DataStack[a].Equal(p.GetFloatConstant(clause[pc++]));

                case OpcodeConstantType.Object:
                    return DataStack[a].EqualReference(p.GetObjectConstant<object>(clause[pc++]));

                case OpcodeConstantType.FunctionalExpression:
                    pc = FunctionalExpression.Eval(p, clause, pc, goalFrame, frameBase, dTop);
                    var resultAddress = dTop + FunctionalExpression.EvalStackOffset;
                    // Goal result is not ad address resultAddress+1
                    var headResultType = DataStack[resultAddress].Type;
                    if (headResultType != DataStack[a].Type)
                        return false;
                    switch (headResultType)
                    {
                        case TaggedValueType.Boolean:
                            if (DataStack[resultAddress].boolean != DataStack[a].boolean)
                                return false;
                            break;
                        case TaggedValueType.Integer:
                            if (DataStack[resultAddress].integer != DataStack[a].integer)
                                return false;
                            break;
                        case TaggedValueType.Float:
                            // ReSharper disable once CompareOfFloatsByEqualityOperator
                            if (DataStack[resultAddress].floatingPoint != DataStack[a].floatingPoint)
                                return false;
                            break;
                        case TaggedValueType.Reference:
                            if (!Equals(DataStack[resultAddress].reference,
                                        DataStack[a].reference))
                                return false;
                            break;
                    }
                    return true;

                default:
                    throw new InvalidOperationException("Invalid constant type");
            }
        }

        internal static ushort Deref(int address)
        {
            return Deref((ushort) address);
        }

        internal static ushort Deref(ushort address)
        {
            while (DataStack[address].Type == TaggedValueType.VariableForward)
                address = DataStack[address].forward;
            return address;
        }

        private static ushort SetVarToConstant(int address, Predicate p, byte[] clause, ushort pc, ushort goalFrame, ushort frameBase, ushort dTop)
        {
            SaveVariable((ushort)address);  // Don't need to deref because this is only called for XFirstVar.
            switch ((OpcodeConstantType) clause[pc++])
            {
                case OpcodeConstantType.Boolean:
                    DataStack[address].Set(clause[pc++] != 0);
                    break;

                case OpcodeConstantType.SmallInteger:
                    var val = (sbyte) clause[pc++];
                    DataStack[address].Set(val);
                    break;

                case OpcodeConstantType.Integer:
                    DataStack[address].Set(p.GetIntConstant(clause[pc++]));
                    break;

                case OpcodeConstantType.Float:
                    DataStack[address].Set(p.GetFloatConstant(clause[pc++]));
                    break;

                case OpcodeConstantType.Object:
                    DataStack[address].SetReference(p.GetObjectConstant<object>(clause[pc++]));
                    break;

                case OpcodeConstantType.FunctionalExpression:
                    pc = FunctionalExpression.Eval(p, clause, pc, goalFrame, frameBase, dTop);
                    DataStack[address] = DataStack[dTop+FunctionalExpression.EvalStackOffset];
                    break;
            }
            return pc;
        }

        private static void Unbind(int address)
        {
            DataStack[address].Type = TaggedValueType.Unbound;
        }
#endregion

        private const int NumericCTypes = 1 << (int)OpcodeConstantType.SmallInteger
                                          | 1 << (int)OpcodeConstantType.Integer
                                          | 1 << (int)OpcodeConstantType.Float;

        private static bool IsNumericCType(OpcodeConstantType cType)
        {
            return ((1 << (int)cType) & NumericCTypes) != 0;
        }

#region Debugging
        [Conditional("DEBUG")]
        // ReSharper disable once UnusedParameter.Local
        private static void DebugConsoleRestartMessage(Predicate goalPredicate,
            // ReSharper disable once UnusedParameter.Local
            Predicate headPredicate,
            // ReSharper disable once UnusedParameter.Local
            ChoicePoint cp)
        {
#if DEBUG
            if (SingleStep)
            {
                StandardError.WriteLine(goalPredicate == null
                    ? $"Restarting top-level call to {headPredicate} at clause {cp.NextClause}"
                    : $"Restarting {goalPredicate}'s call to {headPredicate} at clause {cp.NextClause}");
            }
#endif
        }

        [Conditional("DEBUG")]
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private static void DebugConsoleFailMessage(Predicate headPredicate, 
            // ReSharper disable once UnusedParameter.Local
            ushort goalFrame,
            // ReSharper disable once UnusedParameter.Local
            ushort eTop,
            // ReSharper disable once UnusedParameter.Local
            ushort cTop)
        {
#if DEBUG
            if (SingleStep)
            {
                StandardError.WriteLine("\n\n***FAIL***\n\nFailed call to {0} at", headPredicate);
                DumpStack(goalFrame, eTop, cTop);
                StandardError.WriteLine();
            }
#endif
        }

#if DEBUG
        private static bool CheckDebug(ushort goalFrame, Opcode headInstruction, Opcode goalInstruction, Predicate headPredicate, byte[] head, ushort headPc, ushort eTop, ushort cTop, ushort dTop)
        {
            if (SingleStep)
            {
                StandardError.WriteLine($"Head={headInstruction}, Goal={goalInstruction}");
                DumpStackWithHead(goalFrame, eTop, cTop, dTop, headPredicate, head, headPc);
                StandardError.Write("Debug>");
                switch (StandardInput.ReadLine())
                {
                    case "c":
                    case "continue":
                        SingleStep = false;
                        break;

                    case "break":
                        Debugger.Break();
                        break;

                    case "a":
                    case "abort":
                        return false;
                }
            }
            return true;
        }
#endif

        static void DumpStackWithHead(ushort goalFrame, ushort eTop, ushort cTop, ushort dTop, Predicate headPredicate, byte[] head, ushort headPc)
        {
            //StandardError.Write("Try match: ");
            DumpHead(dTop, headPredicate, head, headPc);
            DumpStack(goalFrame, eTop, cTop);
            GlobalVariable.DumpReportedGlobalValues(StandardError);
        }

        private static void DumpStack(ushort goalFrame, ushort eTop, ushort cTop)
        {
            var environment = eTop - 1;
            var choicePoint = cTop - 1;

            while (choicePoint >= 0 && ChoicePointStack[choicePoint].CallingFrame == goalFrame)
            {
                StandardError.WriteLine($"        next {ChoicePointStack[choicePoint].Callee} clause={ChoicePointStack[choicePoint].NextClause}");
                choicePoint--;
            }

            while (environment >= 0)
            {
                while (choicePoint >= 0 && ChoicePointStack[choicePoint].CallingFrame == environment)
                {
                    StandardError.WriteLine($"        next {ChoicePointStack[choicePoint].Callee} clause={ChoicePointStack[choicePoint].NextClause}");
                    choicePoint--;
                }

                var frame = EnvironmentStack[environment];
                StandardError.Write(frame.Predicate == null
                    ? "Top-level goal"
                    : $"{frame.Predicate.Name}");
                DumpFrame(frame);

                if (choicePoint>=0
                    && ChoicePointStack[choicePoint].CallingFrame==frame.ContinuationFrame
                    && ChoicePointStack[choicePoint].Callee == frame.Predicate)
                {
                    StandardError.WriteLine();
                    StandardError.Write($"        next {ChoicePointStack[choicePoint].Callee} clause={ChoicePointStack[choicePoint].NextClause}");
                    choicePoint--;
                }

                StandardError.WriteLine();
                environment--;
            }
            Debug.Assert(choicePoint<=0);
        }

        private static void DumpHead(ushort dTop, Predicate headPredicate, byte[] head, ushort headPc)
        {
            StandardError.Write(headPredicate.Name);
            if (headPredicate.Arity > 0)
            {
                StandardError.Write('(');
                var firstArg = true;
                for (ushort pc = 0; pc < head.Length; )
                {
                    switch ((Opcode)head[pc++])
                    {
                        case Opcode.HeadVoid:
                            if (firstArg)
                                firstArg = false;
                            else
                                StandardError.Write(", ");

                            StandardError.Write('_');
                            break;

                        case Opcode.HeadVarFirst:
                        case Opcode.HeadVarMatch:
                            if (firstArg)
                                firstArg = false;
                            else
                                StandardError.Write(", ");

                            if (pc >= headPc)
                            {
                                // Don't print it if the instruction loading it hasn't run yet
                                StandardError.Write('*');
                                pc++;
                            }
                            else
                                DumpVar(dTop + head[pc++]);
                            break;

                        case Opcode.HeadConst:
                            if (firstArg)
                                firstArg = false;
                            else
                                StandardError.Write(", ");

                            switch ((OpcodeConstantType)head[pc++])
                            {
                                case OpcodeConstantType.Boolean:
                                    StandardError.Write(head[pc++] != 0);
                                    break;

                                case OpcodeConstantType.SmallInteger:
                                    var val = (sbyte)head[pc++];
                                    StandardError.Write(val);
                                    break;

                                case OpcodeConstantType.Integer:
                                    StandardError.Write(headPredicate.GetIntConstant(head[pc++]));
                                    break;

                                case OpcodeConstantType.Float:
                                    StandardError.Write(headPredicate.GetFloatConstant(head[pc++]));
                                    break;

                                case OpcodeConstantType.Object:
                                    StandardError.Write(ExpressionParser.WriteExpressionToString(headPredicate.GetObjectConstant<object>(head[pc++])));
                                    break;
                            }
                            break;

                        default:
                            goto done;
                    }
                }
                done:
                StandardError.Write(')');
            }
            StandardError.WriteLine();
        }

        private static void DumpVar(int address)
        {
            if (DataStack[address].Type == TaggedValueType.VariableForward && DataStack[address].forward == address)
                StandardError.Write("(self-aliased)");
            else
            {
                address = Deref(address);
                if (DataStack[address].Type == TaggedValueType.Unbound)
                    StandardError.Write("_{0}", address);
                else
                    StandardError.Write(DataStack[Deref(address)]);
            }
        }

        private static void DumpFrame(Environment frame)
        {
            if (frame.CompiledClause.HeadModel != null)
            {
                StandardError.Write('(');
                var firstArg = true;
                foreach (var a in frame.CompiledClause.HeadModel)
                {
                    if (firstArg)
                        firstArg = false;
                    else
                        StandardError.Write(", ");
                    if (a is StackReference)
                    {
                        var offset = ((StackReference) a).Offset;
                        if (offset < 0)
                            StandardError.Write('_');
                        else
                            DumpVar(frame.Base + offset);
                    }
                }
                StandardError.Write(')');
            }
            StandardError.WriteLine();
            if (frame.CompiledClause.Source != null)
                StandardError.Write("     Rule: {0}", Elipsize(ExpressionParser.WriteExpressionToString(frame.CompiledClause.Source)));
        }

        private static string Elipsize(string str, int maxLength = 80)
        {
            if (str.Length > maxLength)
                return str.Substring(0, maxLength-3) + "...";
            return str;
        }

#endregion
    }
}
