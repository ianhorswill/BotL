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
        HeadConst=0,
        HeadVoid=1,
        HeadVarFirst = 2,
        HeadVarMatch = 3,
        // Head/body
        CSpecial=4,
        CNoGoal=5,
        CGoal=6,
        CCut = 7,
        // Body only: 8+
        GoalConst = 1*8,
        GoalVoid = 2*8,
        GoalVarFirst = 3*8,
        GoalVarMatch = 4*8,
        CFail = 5 * 8,
        // These two must come last because of the test for call instructions in the engine
        // (used for tracing)
        CCall = 6 * 8,
        CLastCall = 7 * 8
    }
}
