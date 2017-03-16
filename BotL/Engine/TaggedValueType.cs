#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TaggedValueType.cs" company="Ian Horswill">
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
    /// The type of data stored in a TaggedValue cell
    /// </summary>
    public enum TaggedValueType
    {
        /// <summary>
        /// Value is an integer stored in the integer field of the TaggedValue
        /// </summary>
        Integer,
        /// <summary>
        /// Value is an (single precision) float stored in the floatingPoint field of the TaggedValue
        /// </summary>
        Float,
        /// <summary>
        /// Value is an boolean stored in the boolean field of the TaggedValue
        /// </summary>
        Boolean,
        /// <summary>
        /// TaggedValue is bound to something other than an int, float, or bool.
        /// It's stored in the reference field of the TaggedValue.
        /// </summary>
        Reference,
        /// <summary>
        /// Used only for variables in the DataStack array.  The variable has been aliased to some
        /// other variable whose DataStack index is in the integer field of the TaggedValue.
        /// </summary>
        VariableForward,
        /// <summary>
        /// Used only for variables, i.e. TaggedValues in the DataStack array.  The variable is uninstantiated.
        /// </summary>
        Unbound
    }
}
