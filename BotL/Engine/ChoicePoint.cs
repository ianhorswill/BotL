#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChoicePoint.cs" company="Ian Horswill">
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
    /// <summary>
    /// Saved information describing how to try the next choice for a call to a predicate.
    /// These are kept in a stack, and whenever a predicate fails in BotL, the VM pops the choicepoint
    /// stack and restarts in the state it specifies.
    /// If the stack is empty, then the top-level query has failed.
    /// The choicepoint stack is separate from the environment stack.
    /// </summary>
    internal struct ChoicePoint
    {
        /// <summary>
        /// EnvironmentStack frame for the call this choicepoint corresponds to.
        /// Important: this is the stackframe of the *caller* of the predicate we're restarting.
        /// </summary>
        public readonly ushort CallingFrame;
        /// <summary>
        /// PC to restart the caller at.  When we restart a call, we blow away the old call's frame and
        /// rerun the code that pushes the arguments so we can re-match the arguments (that's how the VAM works).
        /// </summary>
        public readonly ushort CallingPC;
        /// <summary>
        /// The predicate being restarted.
        /// </summary>
        public readonly Predicate Callee;
        /// <summary>
        /// The index of the next clause of the callee that we should try, if this is a rule. The
        /// row number, if it's a table, and a magic cookie to be interpreted by the primop,
        /// if it's a call to a primop.
        /// </summary>
        public ushort NextClause;
        /// <summary>
        /// Stack depth to restore to
        /// </summary>
        public readonly ushort DataStackTop;
        /// <summary>
        /// Undo all trailed entries down to this depth.
        /// </summary>
        public readonly ushort TrailTop;
        /// <summary>
        /// Undo all cleanup records in the undo stack to this depth.
        /// </summary>
        public readonly ushort UndoStackTop;

        public readonly ushort SavedETop;

        public ChoicePoint(ushort callingFrame, ushort callingPc, Predicate callee, ushort nextClause, ushort dTop, ushort trailTop, ushort undoStackTop, ushort savedETop)
        {
            CallingFrame = callingFrame;
            CallingPC = callingPc;
            Callee = callee;
            NextClause = nextClause;
            DataStackTop = dTop;
            TrailTop = trailTop;
            SavedETop = savedETop;
            UndoStackTop = undoStackTop;
            //Debug.Assert(callingFrame==0 || Engine.EnvironmentStack[callingFrame].CompiledClause.Code[callingPc-2]==(byte)Opcode.CGoal, "Invalid calling PC address in choicepoint");
        }

        public override string ToString()
        {
            return $"{CallingFrame}:{Engine.EnvironmentStack[CallingFrame].Predicate}=>{Callee}";
        }
    }
}
