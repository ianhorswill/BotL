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
using BotL.Compiler;
using BotL.Parser;
using static BotL.Repl;

namespace BotL
{
    public static class Engine
    {
        internal static readonly TaggedValue[] DataStack = new TaggedValue[20000];
        internal static readonly Environment[] EnvironmentStack = new Environment[2000];
        static readonly ChoicePoint[] ChoicePointStack = new ChoicePoint[1000];
        private static readonly ushort[] Trail = new ushort[2000];
        internal static ushort TrailTop;

#if DEBUG
        public static bool SingleStep;
#endif

        #region Front end overloads of Run
        private static readonly Symbol TopLevelGoal = Symbol.Intern("top_level_goal");

        private static BindingEnvironment topLevelBindingEnvironment;
        public static bool Run(string goal)
        {
            KB.Predicate(new PredicateIndicator(TopLevelGoal, 0)).Clear();
            topLevelBindingEnvironment = Compiler.Compiler.CompileInternal(new ExpressionParser(TopLevelGoal.Name+" <-- "+goal).Read(), forceVoidVariables: true);
            return Run(TopLevelGoal);
        }

        public static IEnumerable<KeyValuePair<Symbol, object>> TopLevelResultBindings
        {
            get
            {
                foreach (var b in topLevelBindingEnvironment.FrameBindings(0))
                    yield return b;
            }
        }

        public static bool Run(Symbol entryPoint)
        {
            return Run(KB.Predicate(new PredicateIndicator(entryPoint)));
        }
        #endregion

        private static readonly byte[] Trampoline = { (byte)Opcode.CLastCall };

        private static readonly CompiledClause TrampolineRule =
            new CompiledClause(null, Trampoline, 0, null);

        // Just a placeholder so goalPredicate will always be non-null.
        static readonly Predicate TrampolinePredicate = new Predicate(Symbol.Intern("trampoline"), 0);

        private static bool Run(Predicate headPredicate)
        {
            #region Startup
            ushort dTop = 0;         // Data stack pointer
            ushort eTop = 0;         // Environment stack pointer
            ushort cTop = 0;         // Choicepoint stack pointer
            ushort startOfCall = 0;  // Position of start of call to current predicate
            ushort trailSave = 0;    // Trail pointer at start of call
            ushort dTopSave = 0;     // Data stack point at start of call
            ushort restartedClauseNumber = 0;
            TrailTop = 0;

            byte[] goal = Trampoline;
            Predicate goalPredicate = TrampolinePredicate;
            
            if (headPredicate.FirstClause == null)
                return false;

            // Get the first rule
            var headRule = headPredicate.FirstClause;
            byte[] head = headRule.Code;
            ushort goalFrame = eTop++;
            // ReSharper disable once ExpressionIsAlwaysNull
            EnvironmentStack[goalFrame] = new Environment(goalPredicate, TrampolineRule, dTop, 0, cTop, 0);
            ushort headPc = 0;
            ushort goalPc = 0;

            // cTop before the call the to current head.
            // The callee saves this value in the environment upon entry to a clause
            // and then resets cTop to it if/when they perform a cut.
            //
            // We can't just let the callee remember cTop at entry time because
            // the cp stack may or may not have a frame for the callee upon entry.
            ushort callerCp = 0;

            // Allocate new choice frame, if necessary
            if (headPredicate.ExtraClauses != null)
                ChoicePointStack[cTop++] = new ChoicePoint(goalFrame, startOfCall, headPredicate, 0, dTopSave, trailSave, eTop);
            #endregion

            try
            {
                while (true)
                {
                    SanityCheckStack(goalFrame, eTop, cTop, dTop);
                    //Debug.Assert(
                    //    (goal == Trampoline && startOfCall == 0) || goal[startOfCall - 2] == (byte) Opcode.CGoal,
                    //    "Invalid startOfCall value");
                    var headInstruction = (Opcode) head[headPc++];
                    var goalInstruction = (Opcode) goal[goalPc++];

#if DEBUG
                    if (
                        !CheckDebug(goalFrame, headInstruction, goalInstruction, headPredicate, head, headPc, eTop, cTop,
                            dTop)) return false;
#endif
                    if (goalInstruction >= Opcode.CCall && headPredicate.IsTraced)
                    {
                        TraceCall(headPredicate, dTop, head, headPc, headRule);
                    }

                    switch ((int) headInstruction + (int) goalInstruction)
                    {
                            #region Argument matching instructions

                        //
                        // Constant/Constant matching
                        //
                        case (int) Opcode.HeadConst + (int) Opcode.GoalConst:
                            var headCType = (OpcodeConstantType) head[headPc++];
                            var goalCType = (OpcodeConstantType) goal[goalPc++];
                            if (headCType == goalCType)
                            {
                                switch (headCType)
                                {
                                    case OpcodeConstantType.Boolean:
                                    case OpcodeConstantType.SmallInteger:
                                        if (head[headPc++] != goal[goalPc++])
                                            goto fail;
                                        break;

                                    case OpcodeConstantType.Integer:
                                        if (headPredicate.GetIntConstant(head[headPc++])
                                            != goalPredicate.GetIntConstant(goal[goalPc++]))
                                            goto fail;
                                        break;

                                    case OpcodeConstantType.Float:
                                        // ReSharper disable once CompareOfFloatsByEqualityOperator
                                        if (headPredicate.GetFloatConstant(head[headPc++])
                                            != goalPredicate.GetFloatConstant(goal[goalPc++]))
                                            goto fail;
                                        break;

                                    case OpcodeConstantType.Object:
                                        var headValue = headPredicate.GetObjectConstant<object>(head[headPc++]);
                                        var goalValue = goalPredicate.GetObjectConstant<object>(goal[goalPc++]);
                                        if (!Equals(headValue, goalValue))
                                            goto fail;
                                        break;

                                    case OpcodeConstantType.FunctionalExpression:
                                    {
                                        // Get the value of the functional expression
                                        headPc = FunctionalExpression.Eval(headPredicate, head, headPc, dTop, dTop);
                                        var resultAddress = dTop + FunctionalExpression.EvalStackOffset;
                                        goalPc = FunctionalExpression.Eval(goalPredicate, goal, goalPc,
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
                                headPc = FunctionalExpression.Eval(headPredicate, head, headPc, dTop, dTop);
                                var resultAddress = dTop + FunctionalExpression.EvalStackOffset;
                                switch (goalCType)
                                {
                                    case OpcodeConstantType.Boolean:
                                        if (!DataStack[resultAddress].Equal(goal[goalPc++] != 0))
                                            goto fail;
                                        break;

                                    case OpcodeConstantType.SmallInteger:
                                        if (!DataStack[resultAddress].Equal((sbyte) goal[goalPc++]))
                                            goto fail;
                                        break;

                                    case OpcodeConstantType.Integer:
                                        if (
                                            !DataStack[resultAddress].Equal(goalPredicate.GetIntConstant(goal[goalPc++])))
                                            goto fail;
                                        break;
                                    case OpcodeConstantType.Float:
                                        if (
                                            !DataStack[resultAddress].Equal(
                                                goalPredicate.GetFloatConstant(goal[goalPc++])))
                                            goto fail;
                                        break;
                                    case OpcodeConstantType.Object:
                                        if (
                                            !DataStack[resultAddress].EqualReference(
                                                goalPredicate.GetObjectConstant<object>(goal[goalPc++])))
                                            goto fail;
                                        break;
                                }
                            }
                            else if (goalCType == OpcodeConstantType.FunctionalExpression)
                            {
                                goalPc = FunctionalExpression.Eval(goalPredicate, goal, goalPc,
                                    EnvironmentStack[goalFrame].Base, dTop);
                                var resultAddress = dTop + FunctionalExpression.EvalStackOffset;
                                switch (headCType)
                                {
                                    case OpcodeConstantType.Boolean:
                                        if (!DataStack[resultAddress].Equal(head[headPc++] != 0))
                                            goto fail;
                                        break;

                                    case OpcodeConstantType.SmallInteger:
                                        if (!DataStack[resultAddress].Equal((sbyte) head[headPc++]))
                                            goto fail;
                                        break;

                                    case OpcodeConstantType.Integer:
                                        if (
                                            !DataStack[resultAddress].Equal(headPredicate.GetIntConstant(head[headPc++])))
                                            goto fail;
                                        break;
                                    case OpcodeConstantType.Float:
                                        if (
                                            !DataStack[resultAddress].Equal(
                                                headPredicate.GetFloatConstant(head[headPc++])))
                                            goto fail;
                                        break;
                                    case OpcodeConstantType.Object:
                                        if (
                                            !DataStack[resultAddress].EqualReference(
                                                goalPredicate.GetObjectConstant<object>(goal[goalPc++])))
                                            goto fail;
                                        break;
                                }
                            }
                            else if (IsNumericCType(headCType) && IsNumericCType(goalCType))
                            {
                                // Mixed float match
                                // ReSharper disable once CompareOfFloatsByEqualityOperator
                                if (headPredicate.GetFloatConstant(headCType, head[headPc++])
                                    != goalPredicate.GetFloatConstant(goalCType, goal[goalPc++]))
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
                            if ((OpcodeConstantType) goal[goalPc++] == OpcodeConstantType.FunctionalExpression)
                            {
                                // skip to the end of the expression
                                // ReSharper disable once EmptyEmbeddedStatement
                                while ((FOpcode) goal[goalPc++] != FOpcode.Return) ;
                            }
                            else
                                goalPc++;
                            break;

                        case (int) Opcode.HeadConst + (int) Opcode.GoalVoid:
                            // Don't bother reading the constant
                            if ((OpcodeConstantType) head[headPc++] == OpcodeConstantType.FunctionalExpression)
                            {
                                // skip to the end of the expression
                                while ((FOpcode) head[headPc++] != FOpcode.Return)
                                {
                                }
                            }
                            else
                                headPc++;
                            break;

                        case (int) Opcode.HeadVoid + (int) Opcode.GoalVarFirst:
                            // First reference to goal variable is a void variable, so we just initialize
                            // the goal variable to be unbound.
                            Unbind(EnvironmentStack[goalFrame].Base + goal[goalPc++]);
                            break;

                        case (int) Opcode.HeadVarFirst + (int) Opcode.GoalVoid:
                            // First reference to head variable is a void variable, so we just initialize
                            // the head variable to be unbound.
                            Unbind(dTop + head[headPc++]);
                            break;

                        //
                        // Var/Const matching
                        //
                        case (int) Opcode.HeadConst + (int) Opcode.GoalVarFirst:
                            headPc = SetVarToConstant(EnvironmentStack[goalFrame].Base + goal[goalPc++],
                                headPredicate, head, headPc,
                                dTop,
                                dTop);
                            break;

                        case (int) Opcode.HeadVarFirst + (int) Opcode.GoalConst:
                            goalPc = SetVarToConstant(dTop + head[headPc++],
                                goalPredicate, goal, goalPc,
                                EnvironmentStack[goalFrame].Base,
                                dTop);
                            break;

                        case (int) Opcode.HeadConst + (int) Opcode.GoalVarMatch:
                            if (!MatchVarConstant(EnvironmentStack[goalFrame].Base + goal[goalPc++],
                                headPredicate, head, ref headPc,
                                dTop,
                                dTop))
                                goto fail;
                            break;

                        case (int) Opcode.HeadVarMatch + (int) Opcode.GoalConst:
                            if (!MatchVarConstant(dTop + head[headPc++],
                                goalPredicate, goal, ref goalPc,
                                EnvironmentStack[goalFrame].Base,
                                dTop))
                                goto fail;
                            break;

                        //
                        // Var/Var matching
                        //
                        case (int) Opcode.HeadVarFirst + (int) Opcode.GoalVarFirst:
                        {
                            var goalVarAddress = EnvironmentStack[goalFrame].Base + goal[goalPc++];
                            Unbind(goalVarAddress);
                            DataStack[dTop + head[headPc++]].AliasTo(goalVarAddress);
                        }
                            break;

                        case (int) Opcode.HeadVarFirst + (int) Opcode.GoalVarMatch:
                        {
                            var headAddress = (ushort) (dTop + head[headPc++]);
                            var goalAddress = Deref(EnvironmentStack[goalFrame].Base + goal[goalPc++]);
                            Debug.Assert(headAddress != goalAddress, "Aliasing variable to itself");
                            DataStack[headAddress].AliasTo(goalAddress);
                        }
                            break;

                        case (int) Opcode.HeadVarMatch + (int) Opcode.GoalVarFirst:
                        {
                            var goalVarAddress = (ushort) (EnvironmentStack[goalFrame].Base + goal[goalPc++]);
                            Unbind(goalVarAddress);
                            var headVarAddress = dTop + head[headPc++];
                            UnifyDereferenced(goalVarAddress, Deref(headVarAddress));
                        }
                            break;

                        case (int) Opcode.HeadVarMatch + (int) Opcode.GoalVarMatch:
                        {
                            var goalVarAddress = (ushort) (EnvironmentStack[goalFrame].Base + goal[goalPc++]);
                            var headVarAddress = dTop + head[headPc++];
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

                        case (int) Opcode.CFail + (int) Opcode.CLastCall:
                        case (int) Opcode.CFail + (int) Opcode.CCall:
                            goto fail;

                        // Tail call a special predicate
                        case (int) Opcode.CSpecial + (int) Opcode.CLastCall:
                            if (headPredicate.Table != null)
                            {
                                bool canContinue;
                                var nextRow = headPredicate.Table.MatchTableRows(restartedClauseNumber, dTop,
                                    out canContinue);
                                restartedClauseNumber = 0;
                                if (nextRow == 0)
                                    goto fail;
                                if (canContinue)
                                {
                                    ChoicePointStack[cTop++] = new ChoicePoint(goalFrame, startOfCall, headPredicate,
                                        nextRow, dTopSave, trailSave, eTop);
                                }
                            }
                            else
                            {
                                switch (headPredicate.PrimopImplementation(dTop, restartedClauseNumber))
                                {
                                    case CallStatus.Fail:
                                        goto fail;

                                    case CallStatus.DeterministicSuccess:
                                        // Reserve space for temp vars
                                        if (restartedClauseNumber == 0)
                                            dTop += headPredicate.Tempvars;
                                        break;

                                    case CallStatus.NonDeterministicSuccess:
                                        // Reserve space for temp vars
                                        if (restartedClauseNumber == 0)
                                            dTop += headPredicate.Tempvars;
                                        ChoicePointStack[cTop++] = new ChoicePoint(goalFrame, startOfCall,
                                            headPredicate, (byte) (restartedClauseNumber + 1),
                                            dTopSave, trailSave, eTop);
                                        break;

                                    case CallStatus.CallIndirect:
                                        throw new InvalidOperationException("Tail call to primop that returned callindirect.");
                                }
                            }
                            goto lastCallFact;

                        // Tail calling a fact
                        case (int) Opcode.CNoGoal + (int) Opcode.CLastCall:
                            lastCallFact:
                            if (goalPredicate.IsTraced)
                            {
                                TraceSucceed(dTop, headPredicate, head);
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
                                goal = EnvironmentStack[goalFrame].CompiledClause.Code;
                                if (goalPredicate.IsTraced && goalPc == goal.Length)
                                {
                                    TraceSucceed(EnvironmentStack[goalFrame].Base, goalPredicate, goal);
                                }
                            } while (goalPc == goal.Length);

                            continuationLoop:
                            switch ((Opcode) goal[goalPc++])
                            {
                                case Opcode.CGoal:
                                    break;

                                case Opcode.CFail:
                                    goto fail;

                                case Opcode.CCut:
                                    cTop = EnvironmentStack[goalFrame].CallerCTop;
                                    goto continuationLoop;

                                default:
                                    Debug.Assert(false, "Next instruction after continuation should be CGoal");
                                    break;
                            }

                            Debug.Assert(goal[goalPc - 1] == (byte) Opcode.CGoal);
                            headPredicate = goalPredicate.GetObjectConstant<Predicate>(goal[goalPc++]);
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
                            head = headRule.Code;
                            callerCp = cTop;

                            // Allocate new choice frame, if necessary
                            if (headPredicate.ExtraClauses != null)
                                ChoicePointStack[cTop++] = new ChoicePoint(goalFrame, startOfCall,
                                    headPredicate, 0,
                                    dTopSave, trailSave, eTop);
                            break;

                        // (Non-tail) calling a special predicate
                        case (int) Opcode.CSpecial + (int) Opcode.CCall:
                            if (headPredicate.Table != null)
                            {
                                bool canContinue;
                                var nextRow = headPredicate.Table.MatchTableRows(restartedClauseNumber, dTop,
                                    out canContinue);
                                restartedClauseNumber = 0;
                                if (nextRow == 0)
                                    goto fail;
                                if (canContinue)
                                    ChoicePointStack[cTop++] = new ChoicePoint(goalFrame, startOfCall,
                                        headPredicate, nextRow,
                                        dTopSave, trailSave, eTop);
                            }
                            else
                            {
                                switch (headPredicate.PrimopImplementation(dTop, restartedClauseNumber))
                                {
                                    case CallStatus.Fail:
                                        goto fail;

                                    case CallStatus.DeterministicSuccess:
                                        break;

                                    case CallStatus.NonDeterministicSuccess:
                                        ChoicePointStack[cTop++] = new ChoicePoint(goalFrame, startOfCall,
                                            headPredicate, (byte) (restartedClauseNumber + 1),
                                            dTopSave, trailSave, eTop);
                                        break;

                                    case CallStatus.CallIndirect:
                                        headPredicate = (Predicate)DataStack[dTop].reference;
                                        // Goal code falls through to argument instructions w/o an intervening CGoal instruction.
                                        goto beginCall;
                                }
                                // Reserve space for temp vars
                                if (restartedClauseNumber == 0)
                                    dTop += headPredicate.Tempvars;
                            }
                            goto continueCaller;

                        // (Non-tail) calling a fact
                        case (int) Opcode.CNoGoal + (int) Opcode.CCall:
                            // We're done; move on to the next call in goal
                            if (headPredicate.IsTraced)
                            {
                                TraceSucceed(dTop, headPredicate, head);
                            }

                            continueCaller:
                            switch ((Opcode) goal[goalPc++])
                            {
                                case Opcode.CGoal:
                                    headPc = 0;
                                    // ReSharper disable once PossibleNullReferenceException
                                    headPredicate = goalPredicate.GetObjectConstant<Predicate>(goal[goalPc++]);
                                    startOfCall = goalPc;
                                    Debug.Assert(goal[startOfCall - 2] == (byte) Opcode.CGoal, "Invalid startOfCall");
                                    trailSave = TrailTop;
                                    dTopSave = dTop;
                                    if (headPredicate.FirstClause == null)
                                    {
                                        if (headPredicate.IsLocked)
                                            goto fail;
                                        throw new Exception("Undefined predicate: " + headPredicate);
                                    }
                                    headRule = headPredicate.FirstClause;
                                    head = headRule.Code;
                                    callerCp = cTop;
                                    // Allocate new choice frame, if necessary
                                    if (headPredicate.ExtraClauses != null)
                                        ChoicePointStack[cTop++] = new ChoicePoint(goalFrame, startOfCall,
                                            headPredicate, 0,
                                            dTopSave, trailSave, eTop);
                                    break;

                                case Opcode.CCut:
                                    cTop = EnvironmentStack[goalFrame].CallerCTop;
                                    if (goalPc == goal.Length)
                                        // At end of goalPredicate, so return from it.
                                        goto lastCallFact;
                                    goto continueCaller;

                                case Opcode.CFail:
                                    goto fail;

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
                            goal = head;
                            EnvironmentStack[goalFrame].CallerCTop = callerCp;

                            if (!goalPredicate.IsNestedPredicate)
                            {
                                // Allocate space for args
                                EnvironmentStack[goalFrame].Base = dTop;
                                dTop += headRule.EnvironmentSize;
                            } // else base for nested predicate is just the base for the caller.

                            goalPc = headPc;
                            headPredicate = goalPredicate.GetObjectConstant<Predicate>(goal[goalPc++]);
                            startOfCall = goalPc;
                            Debug.Assert(goal[startOfCall - 2] == (byte) Opcode.CGoal, "Invalid startOfCall");
                            trailSave = TrailTop;
                            dTopSave = dTop;

                            if (headPredicate.FirstClause == null)
                            {
                                if (headPredicate.IsLocked)
                                    goto fail;
                                throw new Exception("Undefined predicate: " + headPredicate);
                            }
                            headRule = headPredicate.FirstClause;
                            head = headRule.Code;
                            callerCp = cTop;

                            // Allocate new choice frame, if necessary
                            if (headPredicate.ExtraClauses != null)
                                ChoicePointStack[cTop++] = new ChoicePoint(goalFrame, startOfCall,
                                    headPredicate, 0,
                                    dTopSave, trailSave, eTop);

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
                            goal = head;
                            goalFrame = newFrame;
                            goalPc = headPc;

                            // We know we just fetched a CGoal, so get the predicate being called
                            headPredicate = goalPredicate.GetObjectConstant<Predicate>(goal[goalPc++]);
                            startOfCall = goalPc;
                            Debug.Assert(goal[startOfCall - 2] == (byte) Opcode.CGoal, "Invalid startOfCall");
                            trailSave = TrailTop;
                            dTopSave = dTop;
                            // Fail if it has no rules
                            if (headPredicate.FirstClause == null)
                                goto fail;
                            // Otherwise start matching the head
                            headRule = headPredicate.FirstClause;
                            head = headRule.Code;
                            headPc = 0;
                            callerCp = cTop;

                            // Allocate new choice frame, if necessary
                            if (headPredicate.ExtraClauses != null && head == headPredicate.FirstClause.Code)
                                ChoicePointStack[cTop++] = new ChoicePoint(goalFrame, startOfCall,
                                    headPredicate, 0, dTopSave,
                                    trailSave, eTop);
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
                            UndoTo(cp.TrailTop);
                            trailSave = cp.TrailTop;
                            goalPredicate = EnvironmentStack[goalFrame].Predicate;
                            goal = EnvironmentStack[goalFrame].CompiledClause.Code;
                            startOfCall = goalPc = cp.CallingPC;
                            //Debug.Assert(goalFrame == 0 || goal[startOfCall - 2] == (byte) Opcode.CGoal,
                            //    "Invalid startOfCall");

#if DEBUG
                            if (SingleStep)
                                StandardError.WriteLine($"Restored to dTop={dTop}, TrailTop={TrailTop}");
#endif

                            headPredicate = cp.Callee;
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
                            head = headRule.Code;
                            callerCp = cTop;

                            DebugConsoleRestartMessage(goalPredicate, headPredicate, cp);

                            headPc = 0;

                            #endregion

                            break;
                    }
                }
            }
            catch
            {
                if (StandardError != null)
                    DumpStackWithHead(goalFrame, eTop, cTop, dTop, headPredicate, head, headPc);
                throw;
            }
        }

        private static void TraceSucceed(ushort dTopSave, Predicate goalPredicate, byte[] goal)
        {
            StandardError.Write("Succeed: ");
            DumpHead(dTopSave, goalPredicate, goal, 9999);
        }

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

        #region Trailing
        internal static void SaveUndo(ushort address)
        {
            Trail[TrailTop++] = address;
        }

        internal static void UndoTo(ushort cpTrailTop)
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
            SaveUndo(addressToSet);
            DataStack[addressToSet] = DataStack[addressToRead];
        }

        private static void AliasDereferenced(ushort a1, ushort a2)
        {
            if (a1 == a2)
                return;
            if (a1 > a2)
            {
                SaveUndo(a1);
                DataStack[a1].AliasTo(a2);
            }
            else
            {
                SaveUndo(a2);
                DataStack[a2].AliasTo(a1);
            }
        }

        private static bool MatchVarConstant(int address, Predicate p, byte[] clause, ref ushort pc, ushort frameBase, ushort dTop)
        {
            var a = Deref((ushort)address);
            if (DataStack[a].Type == TaggedValueType.Unbound)
            {
                pc = SetVarToConstant(a, p, clause, pc, frameBase, dTop);
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
                    pc = FunctionalExpression.Eval(p, clause, pc, frameBase, dTop);
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

        private static ushort SetVarToConstant(int address, Predicate p, byte[] clause, ushort pc, ushort frameBase, ushort dTop)
        {
            SaveUndo((ushort)address);  // Don't need to deref because this is only called for XFirstVar.
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
                    pc = FunctionalExpression.Eval(p, clause, pc, frameBase, dTop);
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
        private static void DebugConsoleRestartMessage(Predicate goalPredicate, Predicate headPredicate, ChoicePoint cp)
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
        private static void DebugConsoleFailMessage(Predicate headPredicate, ushort goalFrame, ushort eTop, ushort cTop)
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
            StandardError.Write("Try match: ");
            DumpHead(dTop, headPredicate, head, headPc);
            DumpStack(goalFrame, eTop, cTop);
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
                StandardError.Write("     Rule: {0}", ExpressionParser.WriteExpressionToString(frame.CompiledClause.Source));
        }
        #endregion
    }
}
