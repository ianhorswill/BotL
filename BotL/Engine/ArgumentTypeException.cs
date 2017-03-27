#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ArgumentTypeException.cs" company="Ian Horswill">
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

namespace BotL
{
    /// <summary>
    /// Specialized ArgumentException for when the call to a primop or function involved the wrong type.
    /// </summary>
    public class ArgumentTypeException : ArgumentException
    {
        /// <summary>
        /// Description of the problem
        /// </summary>
        private readonly string message;
        /// <summary>
        /// Name of the BotL primop or function that was called.
        /// </summary>
        private readonly string procName;
        /// <summary>
        /// Position in the argument list of the offending argument.  1=first argument.
        /// </summary>
        private readonly int argumentIndex;
        /// <summary>
        /// Value of the offending argument.
        /// </summary>
        private readonly object argument;

        public ArgumentTypeException(string procName, int argumentIndex, string message, object argument)
        {
            this.procName = procName;
            this.argumentIndex = argumentIndex;
            this.message = message;
            this.argument = argument;
        }

        public override string Message => $"In {procName}, argument {argumentIndex}={argument}: {message}";
    }
}
