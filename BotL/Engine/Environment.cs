#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Environment.cs" company="Ian Horswill">
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
    /// An activation frame for a call to a specific clause of a predicate
    /// </summary>
    internal struct Environment
    {
        /// <summary>
        /// Activation frame for a call to a specific clause of a predicate
        /// </summary>
        /// <param name="p">Predicate being called</param>
        /// <param name="r">Rule being run</param>
        /// <param name="b">Base address on the data stack</param>
        /// <param name="kFrame">Environment frame of the clause that called this caluse</param>
        /// <param name="cp">Choice-point stack pointer before entry to predicate.</param>
        /// <param name="kPc">Continuation PC within the calling clause</param>
        public Environment(Predicate p, CompiledClause r, ushort b, ushort kFrame, ushort cp, ushort kPc)
        {
            Base = b;
            ContinuationFrame = kFrame;
            ContinuationPc = kPc;
            CallerCTop = cp;
            Predicate = p;
            CompiledClause = r;
        }

        /// <summary>
        /// The specifc predicate being executed
        /// </summary>
        public Predicate Predicate;
        /// <summary>
        /// Rule currently being run.
        /// </summary>
        public CompiledClause CompiledClause;

        /// <summary>
        /// Base address of variables on the data stack
        /// </summary>
        public ushort Base;
        /// <summary>
        /// Callign frame
        /// </summary>
        public readonly ushort ContinuationFrame;
        /// <summary>
        /// Continuation PC within the clause of the calling frame
        /// </summary>
        public readonly ushort ContinuationPc;

        /// <summary>
        /// ChoicePoint stack pointer just before entry
        /// </summary>
        public ushort CallerCTop;
        
        public override string ToString()
        {
            return $"{Predicate} => {Engine.EnvironmentStack[ContinuationFrame].Predicate}:{ContinuationPc}";
        }
    }
}
