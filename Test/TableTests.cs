#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TableTests.cs" company="Ian Horswill">
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
    /// Summary description for TableTests
    /// </summary>
    [TestClass]
    public class TableTests
    {
        static TableTests()
        {
            KB.DefineTable("tab", 2);
            KB.AddTableRow("tab", 2, Symbol.Intern("a"), 1);
            KB.AddTableRow("tab", 2, Symbol.Intern("b"), 2);
            KB.AddTableRow("tab", 2, Symbol.Intern("c"), 3);
            KB.AddTableRow("tab", 2, Symbol.Intern("d"), 4);
        }
        
        [TestMethod]
        public void TableOutputTest()
        {
            TestTrue("tab(X,Y)");
            TestTrue("tab(X,Y), true");
        }

        [TestMethod]
        public void TableMatchPositiveTest()
        {
            TestTrue("tab(a,1)");
            TestTrue("tab(a,1), true");
        }

        [TestMethod]
        public void TableMatchNegativeTest()
        {
            TestFalse("tab(a,2)");
            TestFalse("tab(a,2)->true | fail");
        }

        [TestMethod]
        public void TableEnumerateTest()
        {
            TestTrue("forall(tab(X,Y), integer(Y))");
            TestTrue("forall((tab(X,Y), true), integer(Y))");
        }

        [TestMethod]
        public void SetTests()
        {
            Functions.DeclareFunction("settest", 1);
            KB.DefineTable("settest", 2);
            KB.AddTableRow("settest", 2, "a", 1);
            KB.AddTableRow("settest", 2, "b", 2);
            TestTrue("assert(settest(\"c\", 3)), settest(\"c\", 3)");
            TestTrue("set settest(\"a\")=4, settest(\"a\", 4)");
            TestTrue("set settest(\"a\") += 4, settest(\"a\", 8.0)");
        }

        [TestMethod]
        public void AssertTests()
        {
            KB.DefineTable("asserttest", 2);
            KB.AddTableRow("asserttest", 2, "a", 1);
            KB.AddTableRow("asserttest", 2, "b", 2);
            TestFalse("asserttest(a, 2)");
            TestTrue("assert(asserttest(a,2)), asserttest(a, 2)");
        }

        [TestMethod]
        public void RetractTests()
        {
            KB.DefineTable("retracttest", 2);
            KB.AddTableRow("retracttest", 2, "a", 1);
            KB.AddTableRow("retracttest", 2, "b", 2);
            TestTrue("retracttest(\"a\", 1)");
            TestFalse("retract(retracttest(\"a\",1)), retracttest(\"a\", 1)");
            TestTrue("retracttest(\"b\", 2)");
            TestFalse("retractall(retracttest(_,_)), retracttest(\"b\", 2)");
        }

        private void TestFalse(string code)
        {
            Assert.IsFalse(Engine.Run(code));
        }

        private void TestTrue(string code)
        {
            Assert.IsTrue(Engine.Run(code));
        }
    }
}
