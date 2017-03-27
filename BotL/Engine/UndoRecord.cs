#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UndoRecord.cs" company="Ian Horswill">
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
    /// A closure to be called to undo some state change when backtracking 
    /// </summary>
    /// <param name="arg">The UndoRecord containing further information about what to undo.</param>
    delegate void UndoAction(ref UndoRecord arg);

    /// <summary>
    /// A record indicating some action to take to undo some state chance when the system backtracks.
    /// </summary>
    internal struct UndoRecord
    {
        public UndoAction Action;
        public object objArg;
        public int intArg;
        public TaggedValue TaggedArg;

        public void Invoke()
        {
            Action(ref this);
        }

        public void Set(UndoAction a)
        {
            Action = a;
        }

        public void Set(UndoAction a, object o, int i)
        {
            Action = a;
            objArg = o;
            intArg = i;
        }

        public void Set(UndoAction a, object o, ref TaggedValue t)
        {
            Action = a;
            objArg = o;
            TaggedArg = t;
        }
    }
}
