using Microsoft.VisualStudio.TestTools.UnitTesting;
using BotL;

namespace Test
{
    [TestClass]
    public class ELTests
    {
        [TestMethod]
        public void ReadNonExclusiveTest()
        {
            var root = ELNode.Root.StoreNonExclusive(Symbol.Intern("rnet"));
            root.StoreNonExclusive(Symbol.Intern("a"));
            root.StoreNonExclusive(Symbol.Intern("b"));
            TestTrue("/rnet/a");
            TestTrue("/rnet/b");
            TestFalse("/rnet/c");
            TestTrue("/rnet/X, X=a");
            TestTrue("/rnet/X, X=b");
            TestFalse("/rnet/X, X=c");
        }

        [TestMethod]
        public void ReadExclusiveTest()
        {
            ELNode.Store(ELNode.Root / Symbol.Intern("ret") % Symbol.Intern("a"));
            TestTrue("/ret:a");
            TestFalse("/ret:c");
            TestTrue("/ret:X, X=a");
            TestFalse("/ret:X, X=c");

            ELNode.Store(ELNode.Root / Symbol.Intern("ret") % Symbol.Intern("b"));
            TestTrue("/ret:b");
            TestFalse("/ret:c");
            TestTrue("/ret:X, X=b");
            TestFalse("/ret:X, X=c");
        }

        [TestMethod]
        public void ReadRedirectTest()
        {
            ELNode.Store(ELNode.Root / Symbol.Intern("rrt") / Symbol.Intern("a") % 1);
            TestTrue("/rrt >> X, X/a");
            TestTrue("/rrt/a >> X, X:1");
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
