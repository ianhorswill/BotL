using System;
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
    }
}
