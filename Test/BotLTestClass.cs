using System;
using BotL;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test
{
    public class BotLTestClass
    {
        public void TestFalse(string code)
        {
            Assert.IsFalse(Engine.Run(code));
        }

        public void TestTrue(string code)
        {
            Assert.IsTrue(Engine.Run(code));
        }
    }
}
