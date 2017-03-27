#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VariableInfo.cs" company="Ian Horswill">
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
namespace BotL.Compiler
{
    /// <summary>
    /// Represents compiler-internal information about a variable.
    /// </summary>
    class VariableInfo
    {
        public Variable Variable;
        /// <summary>
        /// How many times this variable appears in the head
        /// </summary>
        private int headUses;
        /// <summary>
        /// How many times this variable appears in the body.
        /// </summary>
        public int BodyUses;
        /// <summary>
        /// Have we already compiled the first refernce to this variable?
        /// </summary>
        public bool FirstReferenceCompiled;
        /// <summary>
        /// Position within the run-time environment for the call
        /// </summary>
        public int EnvironmentIndex = -1;

        public VariableInfo(Variable variable)
        {
            Variable = variable;
        }

        /// <summary>
        /// How many times this variable appears in the rule or fact.
        /// </summary>
        public int Uses => headUses + BodyUses;

        /// <summary>
        /// Does this variable only have one use and can therefore be ignored?
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public bool IsSingleton => Uses == 1;

        /// <summary>
        /// Increment use count.
        /// </summary>
        /// <param name="isHead">Whether the use occurs in the head (true) or body (false)</param>
        public void NoteUse(bool isHead)
        {
            if (isHead)
                headUses++;
            else
            {
                BodyUses++;
            }
        }

        /// <summary>
        /// Classify variable based on usage counts.
        /// </summary>
        public VariableType Type
        {
            get
            {
                if (IsSingleton)
                    return VariableType.Void;
                return headUses>0 ? VariableType.Head : VariableType.Body;
            }
        }
    }
}
