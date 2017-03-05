#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PrimopTests.cs" company="Ian Horswill">
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
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BotL;
using BotL.Compiler;

namespace Test
{
    /// <summary>
    /// Summary description for PrimopTests
    /// </summary>
    [TestClass]
    public class PrimopTests : BotLTestClass
    {
        [TestMethod]
        public void OrdinalTests()
        {
            TestTrue("1 < 2");
            TestFalse("1 < 1");
            TestTrue("1 < 2, true");
            TestFalse("1 < 1, true");
            TestFalse("1 < 2, fail");
        }

        [TestMethod]
        public void InArrayTest()
        {
            TestTrue("1 in array(1, 2, 3)");
            TestTrue("2 in array(1, 2, 3)");
            TestFalse("bla in array(1, 2, 3)");

            Compiler.Compile("inatest(X) <-- Y= array(1,2,3), X in Y");
            TestTrue("inatest(X), X=1");
            TestTrue("inatest(X), X=2");
            TestTrue("inatest(X), X=3");
            TestFalse("inatest(X), X=bla");
            TestFalse("X in array()");

            TestTrue("X in array(1, 2, 3), X=1");
            TestTrue("X in array(1, 2, 3), X=2");
            TestFalse("X in array(1, 2, 3), X=bla");

            TestTrue("X in hashset(1,2,3), X=2");
            TestTrue("2 in hashset(1,2,3)");
        }

        [TestMethod]
        public void InQueueTest()
        {
            TestTrue("2 in new Queue(array(1,2,3))");
            TestTrue("X in new Queue(array(1,2,3)), X=2");
            TestFalse("4 in new Queue(array(1,2,3))");
        }

        [TestMethod, ExpectedException(typeof(ArgumentException))]
        public void InNumberTest()
        {
            Engine.Run("1 in 1");
        }

        [TestMethod]
        public void InHashsetTest()
        {
            TestTrue("1 in hashset(1, 2, 3)");
            TestTrue("2 in hashset(1, 2, 3)");
            TestFalse("bla in hashset(1, 2, 3)");

            TestTrue("X in hashset(1, 2, 3), X=1");
            TestTrue("X in hashset(1, 2, 3), X=2");
            TestFalse("X in hashset(1, 2, 3), X=bla");
        }

        [TestMethod]
        public void MinMaxTest()
        {
            Compiler.Compile(@"dumbset(1)
dumbset(2)
dumbset(3)");
            TestTrue("minimum(X,dumbset(X), M), M=1.0");
            TestTrue("maximum(X,dumbset(X), M), M=3.0");
        }

        [TestMethod]
        public void ArgMinMaxTest()
        {
            Compiler.Compile(@"dumbmap(a,1)
dumbmap(b,2)
dumbmap(c,3)");
            TestTrue("arg_min(X,S, dumbmap(X, S), M), M=a");
            TestTrue("arg_max(X, S, dumbmap(X, S), M), M=c");
        }

        [TestMethod]
        public void SumTest()
        {
            TestTrue("sum(X, X in array(1,2,3), S), S=6.0");
        }

        [TestMethod]
        public void ItemTests()
        {
            TestTrue("X=array(1,2,3), X[1]=2");
            TestTrue("X=array(1,2,3), X[1]=Y, Y=2");
        }

        [TestMethod]
        public void InstantiationTestTests()
        {
            TestTrue("var(X)");
            TestFalse("X=Y, Y=a, var(X)");
            TestFalse("var(1)");

            TestFalse("nonvar(X)");
            TestTrue("X=Y, Y=a, nonvar(X)");
            TestTrue("nonvar(1)");
        }

        [TestMethod]
        public void TypeTestTests()
        {
            TestTrue("float(1.0)");
            TestFalse("float(1)");
            TestFalse("float(X)");
            TestTrue("X=Y, Y=1.0, float(X)");

            TestTrue("integer(1)");
            TestFalse("integer(1.5)");
            TestFalse("integer(X)");
            TestTrue("X=Y, Y=1, integer(X)");

            TestTrue("number(1.0)");
            TestTrue("number(1)");
            TestFalse("number(X)");
            TestTrue("X=Y, Y=1.0, number(X)");

            TestTrue("string(\"a\")");
            TestFalse("string(1)");
            TestFalse("string(X)");
            TestTrue("X=Y, Y=\"a\", string(X)");

            TestTrue("symbol(a)");
            TestFalse("symbol(1)");
            TestFalse("symbol(X)");
            TestTrue("X=Y, Y=a, symbol(X)");
        }

        [TestMethod, ExpectedException(typeof(ArgumentException))]
        public void TestLoad()
        {
            Engine.Run("load(1)");
        }

        [TestMethod, ExpectedException(typeof(ArgumentException))]
        public void TestLoadTable()
        {
            Engine.Run("load_table(1)");
        }

        [TestMethod]
        public void WriteTest()
        {
            var writer = new StringWriter();
            Repl.StandardOutput = writer;
            TestTrue("write(1)");
            writer.Flush();
            Assert.AreEqual("1", writer.ToString());

            writer = new StringWriter();
            Repl.StandardOutput = writer;
            TestTrue("writenl(1)");
            writer.Flush();
            Assert.AreEqual("1\r\n", writer.ToString());
        }

        [TestMethod]
        public void SetProperty()
        {
            TestTrue("set_property(xyzzy, \"Name\", \"foo\"), xyzzy.Name = \"foo\"");
        }
    }
}
