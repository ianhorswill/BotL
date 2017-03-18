#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EngineTests.cs" company="Ian Horswill">
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
    public class EngineTests
    {
        public EngineTests()
        {
            // Define the facts foo and bar
            Compiler.Compile("foo");
            Compiler.Compile("bar");
            Compiler.Compile("baz <-- foo");
            Compiler.Compile("doubleFact <-- foo, bar");
            Compiler.Compile("choicepoint <-- fail");
            Compiler.Compile("choicepoint <-- foo");
            Compiler.Compile("complexChoicepoint <-- foo, fail");
            Compiler.Compile("complexChoicepoint <-- foo");

            Compiler.Compile("head_expression_int(1+1)",
                             "head_expression_float(1.0+1)");

            // Tree search stuff
            Compiler.Compile("child(a, b)",
                "child(a, c)",
                "child(b, d)",
                "child(b, e)",
                "child(c, f)",
                "child(c, g)",
                "desc(X, X)",
                "desc(X, Z) <-- child(X, Y), desc(Y, Z)");

            // Graph search stuff
            Compiler.Compile("edge(a,b)");
            Compiler.Compile("edge(b,c)");
            Compiler.Compile("linked(X,Y) <-- edge(X,Y)");
            Compiler.Compile("linked(X,Y) <-- edge(Y,X)");
            Compiler.Compile("con(X,Y) <-- linked(X,Y)");
            Compiler.Compile("con(X,Z) <-- linked(X,Y), con(Y,Z)");
        }

        [TestMethod]
        public void FactTest()
        {
            Assert.IsTrue(Engine.Run("foo"));
        }

        [TestMethod]
        public void SingleFactSubgoalTest()
        {
            Assert.IsTrue(Engine.Run("baz"));
        }

        [TestMethod]
        public void DoubleFactSubgoalTest()
        {
            Assert.IsTrue(Engine.Run("doubleFact"));
        }

        [TestMethod]
        public void TailCallRuleTest()
        {
            Compiler.Compile("tailCallRule <-- baz");
            Assert.IsTrue(Engine.Run("tailCallRule"));
        }

        [TestMethod]
        public void CallRuleTest()
        {
            Compiler.Compile("callRule <-- baz, foo");
            Assert.IsTrue(Engine.Run("callRule"));
        }

        [TestMethod]
        public void BacktrackTest()
        {
            Compiler.Compile("btTestTramp <-- btTest, foo");
            Compiler.Compile("btTest <-- fail");
            Compiler.Compile("btTest <-- fail");
            Compiler.Compile("btTest <-- foo");
            Assert.IsTrue(Engine.Run("btTestTramp"));
        }

        [TestMethod]
        public void BacktrackTest2()
        {
            Compiler.Compile("btTest2 <-- fail");
            Compiler.Compile("btTest2 <-- fail");
            Compiler.Compile("btTest2 <-- foo");
            Assert.IsTrue(Engine.Run("btTest2"));
        }

        [TestMethod]
        public void BacktrackTest3()
        {
            Compiler.Compile("btTest3 <-- fail");
            Compiler.Compile("btTest3 <-- fail");
            Compiler.Compile("btTest3 <-- fail");
            Assert.IsFalse(Engine.Run("btTest3"));
        }

        [TestMethod]
        public void TailChainTest()
        {
            Compiler.Compile("tailChain <-- tailChain1");
            Compiler.Compile("tailChain1 <-- tailChain2");
            Compiler.Compile("tailChain2 <-- tailChain3");
            Compiler.Compile("tailChain3 <-- tailChain4");
            Compiler.Compile("tailChain4 <-- foo");
            Assert.IsTrue(Engine.Run("tailChain"));
        }

        [TestMethod]
        public void SucceedFailTest()
        {
            Compiler.Compile("succeedFail <-- foo, fail");
            Assert.IsFalse(Engine.Run("succeedFail"));
        }

        [TestMethod]
        public void SucceedFailTest2()
        {
            Compiler.Compile("succeedFail2 <-- baz, fail");
            Assert.IsFalse(Engine.Run("succeedFail2"));
        }

        [TestMethod]
        public void UndefinedEntryPointTest()
        {
            Assert.IsFalse(Engine.Run("fail"));
        }

        [TestMethod]
        public void TailCallComplexChoicepointTest()
        {
            Compiler.Compile("tailCallComplexChoicepoint <-- complexChoicepoint");
            Assert.IsTrue(Engine.Run("tailCallComplexChoicepoint"));
        }

        [TestMethod]
        public void SmallIntArgMatchTest()
        {
            Compiler.Compile("smallintArgMatchTest(1)");
            Compiler.Compile("siAMT <-- smallintArgMatchTest(1)");
            Assert.IsTrue(Engine.Run("siAMT"));
        }

        [TestMethod]
        public void IntArgMatchTest()
        {
            Compiler.Compile("intArgMatchTest(1000)");
            Compiler.Compile("iAMT <-- intArgMatchTest(1000)");
            Assert.IsTrue(Engine.Run("iAMT"));
        }

        [TestMethod]
        public void SmallIntArgMismatchTest()
        {
            Compiler.Compile("smallintArgMismatchTest(1)");
            Compiler.Compile("siAMMT <-- smallintArgMismatchTest(0)");
            Assert.IsFalse(Engine.Run("siAMMT"));
        }

        [TestMethod]
        public void IntArgMismatchTest()
        {
            Compiler.Compile("intArgMismatchTest(1000)");
            Compiler.Compile("iAMMT <-- intArgMismatchTest(10000)");
            Assert.IsFalse(Engine.Run("iAMMT"));
        }

        [TestMethod]
        public void FloatArgMatchTest()
        {
            Compiler.Compile("FloatArgMatchTest(1.0)");
            Compiler.Compile("fAMT <-- FloatArgMatchTest(1.0)");
            Assert.IsTrue(Engine.Run("fAMT"));
        }

        [TestMethod]
        public void FloatArgMismatchTest()
        {
            Compiler.Compile("FloatArgMismatchTest(1.0)");
            Compiler.Compile("fAMMT <-- FloatArgMismatchTest(0.0)");
            Assert.IsFalse(Engine.Run("fAMMT"));
        }

        [TestMethod]
        public void FloatArgMatchPromotionTest()
        {
            Compiler.Compile("FloatArgMatchPromotionTest(1)");
            Compiler.Compile("fAMPT <-- FloatArgMatchPromotionTest(1.0)");
            Assert.IsTrue(Engine.Run("fAMPT"));
        }

        [TestMethod]
        public void FloatArgMismatchPromotionTest()
        {
            Compiler.Compile("FloatArgMismatchPromotionTest(1)");
            Compiler.Compile("fAMMPT <-- FloatArgMismatchPromotionTest(2.0)");
            Assert.IsFalse(Engine.Run("fAMMPT"));
        }

        [TestMethod]
        public void ArgTypeMismatchTest()
        {
            Compiler.Compile("ArgTypeMismatchTest(1)");
            Compiler.Compile("aTMT <-- ArgTypeMismatchTest(foo)");
            Assert.IsFalse(Engine.Run("aTMT"));
        }

        [TestMethod]
        public void ObjectArgMatchTest()
        {
            Compiler.Compile("objectArgMatchTest(foo)");
            Compiler.Compile("oAMT <-- objectArgMatchTest(foo)");
            Assert.IsTrue(Engine.Run("oAMT"));
        }

        [TestMethod]
        public void ObjectArgMismatchTest()
        {
            Compiler.Compile("objectArgMismatchTest(foo)");
            Compiler.Compile("oAMMT <-- objectArgMatchTest(bar)");
            Assert.IsFalse(Engine.Run("oAMMT"));
        }

        [TestMethod]
        public void VhcgTest()
        {
            Compiler.Compile("vhcg(_)");
            Compiler.Compile("vhcgt <-- vhcg(bar)");
            Assert.IsTrue(Engine.Run("vhcgt"));
        }

        [TestMethod]
        public void GraphConnectivityTest()
        {
            Compiler.Compile("graphctest<-- con(a,c)");
            Assert.IsTrue(Engine.Run("graphctest"));
        }

        [TestMethod]
        public void GraphReverseConnectivityTest()
        {
            Compiler.Compile("graphrtest<-- con(c,a)");
            Assert.IsTrue(Engine.Run("graphrtest"));
        }

        [TestMethod]
        public void TreeTest()
        {
            Compiler.Compile("treetest <-- desc(a,f)");
            Assert.IsTrue(Engine.Run("treetest"));
        }

        [TestMethod]
        public void TreeReverseTest()
        {
            Compiler.Compile("treertest <-- desc(f,a)");
            Assert.IsFalse(Engine.Run("treertest"));
        }

        [TestMethod]
        public void DisjunctionLastCallTest()
        {
            TestFalse("false_a | false_b");
            TestTrue("foo | false_b");
            TestTrue("false_a | foo");
        }

        [TestMethod]
        public void DisjunctionNormalCallTest()
        {
            TestFalse("foo, false_a | false_b, foo");
            TestTrue("foo, foo | false_b, foo");
            TestTrue("foo, false_a | foo, foo");
        }

        [TestMethod]
        public void RunFailTest()
        {
            Assert.IsFalse(Engine.Run(Symbol.Intern("fail")));
        }

        [TestMethod]
        public void RunMulticlauseTest()
        {
            Assert.IsTrue(Engine.Run(Symbol.Intern("choicepoint")));
        }

        [TestMethod]
        public void FunctionArgumentMatchingTests()
        {
            TestTrue("head_expression_int(1+1)");
            TestFalse("head_expression_int(1+2)");
            TestTrue("head_expression_int(2)");
            TestFalse("head_expression_int(3)");
            TestFalse("head_expression_int(30000)");
            TestFalse("head_expression_int(foo)");
            TestTrue("head_expression_int(_)");
            TestTrue("head_expression_float(1.0+1)");
            TestTrue("head_expression_float(2.0)");
            TestFalse("head_expression_float(3.0)");
            TestFalse("head_expression_float(3.0+1)");
            TestFalse("head_expression_float(foo)");

            Compiler.Compile("small_int(1)", "large_int(1000)", "fp(1.0)");
            TestTrue("small_int(1+0)");
            TestFalse("small_int(1+1)");
            TestTrue("large_int(1000+0)");
            TestFalse("large_int(1000+1)");
            TestTrue("fp(1.0+0)");
            TestFalse("fp(1.0+1)");
            TestTrue("X=999999, X=999999");
        }

        [TestMethod]
        public void HeadVoidTests()
        {
            Compiler.Compile("head_void(_)");
            TestTrue("head_void(_)");
            TestTrue("head_void(1)");
            TestTrue("X=1, head_void(X)");
            TestTrue("head_void(1+1)");
            TestTrue("_=_");
            Compiler.Compile("num(1)");
            TestTrue("num(_)");

            Compiler.Compile("head_void(_,_)");
            TestTrue("head_void(X,X)");
        }

        [TestMethod]
        public void FactorialTests()
        {
            Compiler.Compile("fact(1,1) <-- !\nfact(N, R) <-- N > 1, fact(N-1, T), R = N*T");
            TestTrue("fact(1,1)");
            TestTrue("fact(3,6)");
            TestFalse("fact(3,7)");
            TestFalse("fact(-1,7)");
        }

        [TestMethod]
        public void CutTests()
        {
            TestTrue("foo, !, foo");
            TestFalse("foo, !, false");

            TestTrue("baz, !, baz");
            TestFalse("baz, !, false");
        }

        [TestMethod]
        public void HVarFirstGVarMatchTest()
        {
            TestTrue("1 = X, X = 1");
        }

        [TestMethod]
        public void NestedChoicepointTest()
        {
            Compiler.Compile(@"g(a,b)
g(a, c)
a(X,Y) <-- g(X,Y)
a(X,Y) <-- g(Y,X)");
            TestFalse("a(a,Z), a(Z, q)");
        }

        [TestMethod]
        public void AliasingTest()
        {
            TestTrue("X=Y, Y=Z, Z=W, W=1, X=1");
            TestTrue("Y=X, Y=Z, Z=W, W=1, X=1");
            TestTrue("X=Y, Z=Y, Z=W, W=1, X=1");
            TestTrue("X=Y, Y=Z, Z=W, 1=W, X=1");
            TestTrue("X=Y, Y=Z, Z=W, W=1, 1=X");
        }

        [TestMethod]
        public void VariableComparisonTests()
        {
            TestFalse("X=1, Y=a, X=Y");
            TestTrue("X=1, Y=1, X=Y");
            TestTrue("X=1.0, Y=1.0, X=Y");
            TestTrue("X=true, Y=true, X=Y");
        }

        [TestMethod]
        public void VarMatchConstantTests()
        {
            TestTrue("X=1, X=1");
            TestFalse("X=1, X=10");
            TestTrue("X=1.0, X=1.0");
            TestFalse("X=1.0, X=10.0");
            TestTrue("X=1, X=1+0");
            TestFalse("X=1, X=10+0");
            TestTrue("X=1.0, X=1.0+0.0");
            TestFalse("X=1.0, X=10.0+0.0");
            TestTrue("X=\"1\", X=((1).ToString())");
            TestTrue("X=true, X=(1).Equals(1)");
            TestTrue("X=(1).Equals(1), X=true");
        }

        [TestMethod]
        public void FEMatchTest()
        {
            TestTrue("X=\"x\", X=(\"xx\".Substring(1))");

            Compiler.Compile("fetest(\"xx\".Substring(1))");
            TestTrue("fetest(\"xx\".Substring(1))");
        }

        [TestMethod]
        public void UnifyTests()
        {
            TestTrue("X=1, Y=X, Y=1");
            TestTrue("X=1, X=Y, Y=1");
        }

        [TestMethod]
        public void MetaCallTest()
        {
            TestTrue("call(con, a, c)");
            TestTrue("call(con, a, X), X=c");
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
