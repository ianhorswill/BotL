#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CompiledClause.cs" company="Ian Horswill">
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
    /// The compiled form of a rule.
    /// Contains a bytecode vector as well as environment size and debug info.
    /// </summary>
    sealed class CompiledClause
    {
        /// <summary>
        /// Source code for the rule.
        /// </summary>
        public readonly object Source;
        /// <summary>
        /// Compiled bytecode for the rule.
        /// </summary>
        public readonly byte[] Code;
        /// <summary>
        /// Number of entries in this rule's stack frame.
        /// </summary>
        public readonly ushort EnvironmentSize;
        /// <summary>
        /// Describes the mapping from head arguments to stack positions
        /// HeadModel[i] = constant, if that arg was a constant in the head
        /// HeadModel[i] = StackReference object if it was a variable.
        /// </summary>
        public readonly object[] HeadModel;

        public CompiledClause(object source, byte[] code, ushort environmentSize, object[] headModel)
        {
            Source = source;
            Code = code;
            EnvironmentSize = environmentSize;
            HeadModel = headModel;
        }

        public override string ToString()
        {
            return $"CompiledClause<{Source}>";
        }
    }
}
