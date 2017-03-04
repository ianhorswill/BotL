using System;
using BotL;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test
{
    [TestClass]
    public class GrammarTests : BotLTestClass
    {
        static GrammarTests()
        {
            KB.Compile("match_literal --> \"a b c\"");

            KB.Compile("s --> np, vp");
            KB.Compile("np --> \"john\"");
            KB.Compile("np --> \"mary\"");
            KB.Compile("np --> \"cats\"");
            KB.Compile("vp --> v, np");
            KB.Compile("s --> v");
            KB.Compile("v --> \"loves\"");
        }

        [TestMethod]
        public void MatchLiteral()
        {
            TestTrue("Q = queue(a, b, c), match_literal(Q), length(Q)=0");
            TestTrue("Q = queue(a, b, c, d), match_literal(Q), length(Q)=1");
            TestFalse("Q=queue(a,b,b), match_literal(Q)");
        }

        [TestMethod]
        public void GenerateLiteral()
        {
            TestTrue("Q=queue(), set_generate_mode(Q), match_literal(Q), dequeue(Q)=a, dequeue(Q)=b, dequeue(Q)=c, length(Q)=0");
        }

        [TestMethod]
        public void GenerateComplex()
        {
            TestTrue("Q=queue(), set_generate_mode(Q), s(Q), dequeue(Q)=john, dequeue(Q)=loves, dequeue(Q)=john, length(Q)=0");
        }

        [TestMethod]
        public void MatchRecursive()
        {
            KB.Compile("string_of(X) --> word(X), string_of(X)");
            KB.Compile("string_of(X) --> \"\"");
            TestTrue("Q=queue(a,a,a,a,a), string_of(a, Q), length(Q)=0");
            TestFalse("Q=queue(a,a,b,a,a), string_of(a, Q), length(Q)=0");
        }

        [TestMethod]
        public void ComplexMatch()
        {
            KB.Compile("sentence(Q) <-- s(Q), length(Q)=0");
            TestTrue("sentence(queue(john, loves, mary))");
            TestTrue("sentence(queue(mary, loves, john))");
            TestTrue("sentence(queue(mary, loves, cats))");
            TestFalse("sentence(queue(loves, john))");
            TestFalse("sentence(queue(john, loves, cats, foo, bar))");
        }
    }
}
