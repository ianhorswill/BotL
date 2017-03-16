#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Opcode.cs" company="Ian Horswill">
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
namespace BotL
{
    public enum Opcode
    {
        // Head opcodes: 0-8

        // Head only

        /// <summary>
        /// Format: HeadConst FOpcodes ...
        /// Callee rule requires a specific value for this argument.
        /// The value expected is determined by the functional expression code after this opcode.
        /// </summary>
        HeadConst=0,
        /// <summary>
        /// Format: HeadVoid
        /// Callee rule will ignore whatever value is passed for this argument
        /// </summary>
        HeadVoid=1,
        /// <summary>
        /// Format: HeadVarFirst Index
        /// Callee rule will bind this argument to a variable at the specified index in its frame.
        /// Used when this is the first use of that variable by the caller.
        /// Hence we don't need to do any matching; we just store the caller's value into this variable.
        /// </summary>
        HeadVarFirst = 2,
        /// <summary>
        /// Format: HeadVarMatch Index
        /// Callee rule expects the value of this argument to match the value of the previously initialized
        /// variable at the specified index in its frame.
        /// </summary>
        HeadVarMatch = 3,

        // Can appear in the head or body

        /// <summary>
        /// Format: headcodes... CSpecial
        /// Call into the code for a table or primop
        /// Tables and primops have fake rules that are there just to
        /// receive and store the arguments being passed to them.  They consist
        /// only of the head opcodes followed by CSpecial.
        /// MAY ONLY APPEAR AS THE LAST INSTRUCTION OF A RULE.
        /// </summary>
        CSpecial=4,
        /// <summary>
        /// Format: headcodes ... NoGoal
        /// Causes rule to succeed, i.e. terminate successfully.
        /// Appears only in rules that have no subgoals (i.e. facts), and is the only opcode
        /// after the head opcodes in such rules.
        /// MAY ONLY APPEAR AS THE LAST INSTRUCTION OF A RULE
        /// </summary>
        CNoGoal=5,
        /// <summary>
        /// Format: CGoal Index GoalArgumentCode ... CallInstruction
        ///         CGoal 255 FOpcodes ... GoalArgumentCode ... CallInstruction
        /// Marks the start of a subgoal in the body of a rule.
        /// Index is the index in the constant table of the predicate to call.
        /// If Index is 255, then this is an indirect call and Index is followed by a functional expression
        /// to compute the predicate to call.
        /// Is followed by GoalX opcodes to pass arguments, and then CCall or CLastCall.
        /// </summary>
        CGoal=6,
        /// <summary>
        /// Format: Cut
        /// Cut; removes any choicepoints including for this predicate and any predicates it's called.
        /// </summary>
        CCut = 7,

        // Body only: 8+

        /// <summary>
        /// Format: GoalConst FOpcodes ...
        /// Caller is passing a constant for this argument.  The value is determined by the functional 
        /// expression code following this opcode
        /// </summary>
        GoalConst = 1*8,
        /// <summary>
        /// Format: GoalVoid
        /// Caller doesn't care about the value of this argument.  Conceptually, it's passing an unbound
        /// singleton variable.
        /// </summary>
        GoalVoid = 2*8,
        /// <summary>
        /// Format: GoalVarFirst Index
        /// Caller is passing the variable at the specified Index in its frame.  Used when this
        /// is the first use of that variable within the caller's code, and so the variable is not 
        /// otherwise initialized.
        /// </summary>
        GoalVarFirst = 3*8,
        /// <summary>
        /// Format: GoalVarMatch Index
        /// Caller is passing the variable at the specified position in the caller's frame as this argument.
        /// Used when this is not the first usage of the variable in the caller, and hence this variable
        /// is known to have already been initialized.
        /// </summary>
        GoalVarMatch = 4*8,
        /// <summary>
        /// Format: CFail
        /// Forces the current rule to fail.
        /// MAY ONLY APPEAR AS THE LAST INSTRUCTION OF A RULE
        /// </summary>
        CFail = 5 * 8,

        // These two must come last because of the test for call instructions in the engine
        // (used for tracing)

        /// <summary>
        /// Format: CGoal ArgumentCode ... CCall MoreStuff
        /// Transfers control to the predicate being called, instructing it to return to this rule
        /// if it succeeds.
        /// Must be followed by subsequent body code (more calls, cut, fail, or CNogoal).
        /// </summary>
        CCall = 6 * 8,
        /// <summary>
        /// Format: CGoal ArgumentCode ... CLastCall
        /// Transfers control to the predicate being called, instructing it to return to our caller's
        /// continuation if it succeeds.
        /// MAY ONLY APPEAR AS THE LAST INSTRUCTION OF A RULE.
        /// </summary>
        CLastCall = 7 * 8
    }
}
