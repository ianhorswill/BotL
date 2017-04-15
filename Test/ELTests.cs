#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ELTests.cs" company="Ian Horswill">
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
    [TestClass]
    public class ELTests : BotLTestClass
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
        public void ReadMixedModeTest()
        {
            TestTrue("assert(/rmm/a:1)");
            TestTrue("assert(/rmm/b:2)");
            TestTrue("assert(/rmm/c:3)");
            TestTrue("/rmm/A:N, A=c");
        }

        [TestMethod]
        public void ForallELTest()
        {
            TestTrue("assert(/foo)");
            TestTrue("assert(/fael/a:1)");
            TestTrue("assert(/fael/b:2)");
            TestTrue("assert(/fael/c:3)");
            TestTrue("forall(/rmm/A:N, true)");
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

        [TestMethod]
        public void ChildValue()
        {
            ELNode.Store(ELNode.Root/"ChildValue"%10);
            Assert.AreEqual(10, (ELNode.Root/"ChildValue").ChildValue);
            ELNode.Store(ELNode.Root / "ChildValue" % "foo");
            Assert.AreEqual("foo", (ELNode.Root / "ChildValue").ChildValue);
        }

        [TestMethod]
        public void ChildIntValue()
        {
            ELNode.Store(ELNode.Root / "ChildIntValue" % 10);
            Assert.AreEqual(10, (ELNode.Root / "ChildIntValue").ChildIntValue);
        }

        [TestMethod]
        public void ChildFloatValue()
        {
            ELNode.Store(ELNode.Root / "ChildFloatValue" % 10.0);
            Assert.AreEqual(10.0, (ELNode.Root / "ChildFloatValue").ChildFloatValue);
            ELNode.Store(ELNode.Root / "ChildFloatValue" % 10);
            Assert.AreEqual(10.0, (ELNode.Root / "ChildFloatValue").ChildFloatValue);
        }

        [TestMethod]
        public void ChildBoolValue()
        {
            ELNode.Store(ELNode.Root / "ChildBoolValue" % true);
            Assert.IsTrue((ELNode.Root / "ChildBoolValue").ChildBoolValue);
        }

        [TestMethod]
        public void ELBurnIn()
        {
            Compiler.Compile(@"
clear_el <-- 
   ignore(retract(/sklerb)),
   ignore(retract(/burnin));
write_el <--
   forall(enumerate_for_el(X,Y),
          assert(/burnin/X:Y)),
   !,
   forall(/burnin/X:Y,
          assert(/sklerb/X:Y));");
            for (int i = 0; i < 100; i++)
            {
                Compiler.Compile($"enumerate_for_el({i}, {i});");
                TestTrue("clear_el, write_el");
            }
        }
    }
}
