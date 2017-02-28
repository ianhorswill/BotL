using System;
using BotL;
using BotL.Compiler;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test
{
    [TestClass]
    public class StructureTests
    {
        public StructureTests()
        {
            Compiler.Compile(@"struct a(B, C)
signature s_test(a, a)
s_test(a(X, Y), a(X, Y))");
        }

        [TestMethod]
        public void ScalarTest()
        {
            TestTrue("s_test(1, 1)");
        }

        [TestMethod]
        public void UnpackTest()
        {
            TestTrue("s_test(a(1,2), a(1,X)), X=2");
        }

        private void TestTrue(string code)
        {
            Assert.IsTrue(Engine.Run(code));
        }
    }
}
