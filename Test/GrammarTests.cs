#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GrammarTests.cs" company="Ian Horswill">
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
using BotL;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test
{
    [TestClass]
    public class GrammarTests : BotLTestClass
    {
        static GrammarTests()
        {
            KB.Compile("match_literal --> \"a b c\"");

            KB.Compile("s --> np, vp");
            KB.Compile("np --> \"john\"");
            KB.Compile("np --> \"mary\"");
            KB.Compile("np --> \"cats\"");
            KB.Compile("vp --> v, np");
            KB.Compile("s --> v");
            KB.Compile("v --> \"loves\"");
        }

        [TestMethod]
        public void MatchLiteral()
        {
            TestTrue("Q = queue(a, b, c), match_literal(Q), length(Q)=0");
            TestTrue("Q = queue(a, b, c, d), match_literal(Q), length(Q)=1");
            TestFalse("Q=queue(a,b,b), match_literal(Q)");
        }

        [TestMethod]
        public void GenerateLiteral()
        {
            TestTrue("Q=queue(), set_generate_mode(Q), match_literal(Q), dequeue(Q)=a, dequeue(Q)=b, dequeue(Q)=c, length(Q)=0");
        }

        [TestMethod]
        public void GenerateComplex()
        {
            TestTrue("Q=queue(), set_generate_mode(Q), s(Q), dequeue(Q)=john, dequeue(Q)=loves, dequeue(Q)=john, length(Q)=0");
        }

        [TestMethod]
        public void MatchRecursive()
        {
            KB.Compile("string_of(X) --> word(X), string_of(X)");
            KB.Compile("string_of(X) --> \"\"");
            TestTrue("Q=queue(a,a,a,a,a), string_of(a, Q), length(Q)=0");
            TestFalse("Q=queue(a,a,b,a,a), string_of(a, Q), length(Q)=0");
        }

        [TestMethod]
        public void ComplexMatch()
        {
            KB.Compile("sentence(Q) <-- s(Q), length(Q)=0");
            TestTrue("sentence(queue(john, loves, mary))");
            TestTrue("sentence(queue(mary, loves, john))");
            TestTrue("sentence(queue(mary, loves, cats))");
            TestFalse("sentence(queue(loves, john))");
            TestFalse("sentence(queue(john, loves, cats, foo, bar))");
        }

        [TestMethod]
        public void MatchCurlyBraces()
        {
            KB.Compile("digits --> word(W), { W in array(0, 1, 2, 3, 4, 5, 6, 7, 8, 9) }, digits");
            KB.Compile("digits --> \"\"");
            TestTrue("Q=queue(1, 5, 9, 1), digits(Q), length(Q)=0");
            TestFalse("Q=queue(1, 5, 9, a, 1), digits(Q), length(Q)=0");
        }
    }
}
