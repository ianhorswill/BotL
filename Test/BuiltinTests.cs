﻿using System;
using System.Text;
using System.Collections.Generic;
using BotL;
using BotL.Compiler;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test
{
    /// <summary>
    /// Summary description for BuiltinTests
    /// </summary>
    [TestClass]
    public class BuiltinTests : BotLTestClass
    {
        [TestMethod]
        public void FailTest()
        {
            Compiler.Compile("failTest <-- fail");
            Assert.IsFalse(Engine.Run("failTest"));
        }
        
        [TestMethod]
        public void InstantiationTestTests()
        {
            TestTrue("var(X)");   // Compiled away
            TestTrue("X=Y, var(X)");
            TestFalse("X=Y, Y=a, var(X)");
            TestFalse("var(1)");  // Compiled away

            TestFalse("nonvar(X)");   // Compiled away
            TestTrue("X=Y, Y=a, nonvar(X)");
            TestFalse("X=Y, nonvar(X)");
            TestTrue("nonvar(1)");  // Compiled away
        }

        [TestMethod]
        public void MinimizeMaximize()
        {
            Compiler.Compile(@"dumbset(1)
dumbset(2)
dumbset(3)");
            TestTrue("minimum(X,dumbset(X), M), M=1.0");
            TestTrue("maximum(X,dumbset(X), M), M=3.0");
        }

        [TestMethod]
        public void ArgMinMaxOneArg()
        {
            Compiler.Compile(@"dumbmap(a,1)
dumbmap(b,2)
dumbmap(c,3)");
            TestTrue("arg_min(X,S, dumbmap(X, S), M), M=a");
            TestTrue("arg_max(X, S, dumbmap(X, S), M), M=c");
        }

        [TestMethod]
        public void ArgMinMaxTwoArgs()
        {
            Compiler.Compile(@"dumbmap2(a,b,1)
dumbmap2(b, c, 2)
dumbmap2(c, d, 3)");
            TestTrue("arg_min((X, Y), S, dumbmap2(X, Y, S), (MX, MY)), MX=a, MY=b");
            TestTrue("arg_max((X, Y), S, dumbmap2(X, Y, S), (MX, MY)), MX=c, MY=d");
        }

        [TestMethod]
        public void Sum()
        {
            TestTrue("sum(X, X in array(1,2,3), S), S=6.0");
        }

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
        public void TypeTestTests()
        {
            TestTrue("float(1.0)");
            TestFalse("float(1)");
            TestFalse("float(X)");
            TestTrue("X=Y, Y=1.0, float(X)");
            TestFalse("X=Y, float(X)");

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
    }
}
