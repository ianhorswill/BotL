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

        [TestMethod]
        public void AssertTest()
        {
            TestTrue("assert(/atest/foo/bar), /atest/foo/bar");
            TestTrue("assert(/atest/a:b), /atest/a:X, X=b");
        }

        [TestMethod]
        public void DeleteTest()
        {
            TestTrue("assert(/dtest/foo/bar), /dtest/foo/bar");
            TestTrue("retract(/dtest/foo)");
            TestFalse("/dtest/foo");
            TestTrue("assert(/dtest/foo/baz), /dtest/foo/baz");
            TestFalse("/dtest/foo/bar");
            TestTrue("assert(/dtest/bar/foo1), assert(/dtest/bar/foo1), retract(/dtest/bar/foo1), not(/dtest/bar/foo1)");
            TestTrue(
                "assert(/dtest/baz/foo1), assert(/dtest/baz/foo2), assert(/dtest/baz/foo3), retract(/dtest/baz/foo2)");
            TestFalse("/dtest/baz/foo2");
            TestTrue("not(/dtest/baz/foo2)");
            TestTrue("/dtest/baz/foo3");
        }

        [TestMethod]
        public void ExclusiveWriteTest()
        {
            TestTrue("assert(/ewtest/a:1)");
            TestTrue("/ewtest/a:1");
            TestTrue("assert(/ewtest/a:2)");
            TestTrue("/ewtest/a:2");
            TestFalse("/ewtest/a:1");
            TestTrue("not(/ewtest/a:1)");
            TestTrue("assert(/ewtest/a:1/foo)");
            TestTrue("/ewtest/a:1/foo");
            TestTrue("assert(/ewtest/a:2)");
            TestTrue("/ewtest/a:2");
            TestFalse("/ewtest/a:2/foo");
            TestTrue("assert(/ewtest/a:3 >> X), assert(X/foo), /ewtest/a:3/foo");
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
