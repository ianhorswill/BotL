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
    internal struct ChoicePoint
    {
        public readonly ushort CallingFrame;
        public readonly ushort CallingPC;
        public readonly Predicate Callee;
        public ushort NextClause;
        public readonly ushort DataStackTop;
        public readonly ushort TrailTop;

        public readonly ushort SavedETop;

        public ChoicePoint(ushort callingFrame, ushort callingPc, Predicate callee, ushort nextClause, ushort dTop, ushort trailTop, ushort savedETop)
        {
            CallingFrame = callingFrame;
            CallingPC = callingPc;
            Callee = callee;
            NextClause = nextClause;
            DataStackTop = dTop;
            TrailTop = trailTop;
            SavedETop = savedETop;
            //Debug.Assert(callingFrame==0 || Engine.EnvironmentStack[callingFrame].CompiledClause.Code[callingPc-2]==(byte)Opcode.CGoal, "Invalid calling PC address in choicepoint");
        }

        public override string ToString()
        {
            return string.Format("{0}:{1}=>{2}",
                CallingFrame,
                Engine.EnvironmentStack[CallingFrame].Predicate,
                Callee);
        }
    }
}
