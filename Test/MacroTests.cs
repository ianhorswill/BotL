#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MacroTests.cs" company="Ian Horswill">
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BotL;
using BotL.Compiler;

namespace Test
{
    /// <summary>
    /// Summary description for MacroTests
    /// </summary>
    [TestClass]
    public class MacroTests : BotLTestClass
    {
        [TestMethod]
        public void NotTests()
        {
            TestTrue("not(fail)");
            TestFalse("not(true)");
        }

        [TestMethod]
        public void AllSolutionsTests()
        {
            Compiler.Compile(@"setoftest(1)
setoftest(2)");
            TestTrue("S=setof(X:setoftest(X)), 1 in S");
            TestTrue("S=listof(X:setoftest(X)), 1 in S");
        }

        [TestMethod]
        public void IgnoreTest()
        {
            TestTrue("ignore(fail)");
        }

        [TestMethod]
        public void OnceTest()
        {
            TestFalse("once(X in array(1,2)), X=2");
        }

        [TestMethod]
        public void ForAllTest()
        {
            TestTrue("forall(X in array(1,2,3), integer(X))");
            TestFalse("forall(X in array(1,a,3), integer(X))");
            TestTrue("forall(false, false)");
        }

        [TestMethod]
        public void CheckSucceedTest()
        {
            TestTrue("check(1=1)");
        }

        [TestMethod, ExpectedException(typeof(CallFailedException))]
        public void CheckFailTest()
        {
            TestFalse("check(1=0)");
        }
    }
}
