﻿#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OpcodeConstantType.cs" company="Ian Horswill">
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
    /// Extension to the HeadConst/GoalConst opcodes, specifying the type of constant to be passed.
    /// </summary>
    enum OpcodeConstantType : byte
    {
        /// <summary>
        /// Value is a constant from the Object constant table
        /// </summary>
        Object,
        /// <summary>
        /// Value is a boolean
        /// </summary>
        Boolean,
        /// <summary>
        /// Value is an sbyte to be converted to an int.
        /// </summary>
        SmallInteger,
        /// <summary>
        /// Value is a constant from the integer constant table
        /// </summary>
        Integer,
        /// <summary>
        /// Value is a constant from the float constant table
        /// </summary>
        Float,
        /// <summary>
        /// Value is to be computed at run-time using the compiled functional code after this opcode.
        /// </summary>
        FunctionalExpression
    }
}
