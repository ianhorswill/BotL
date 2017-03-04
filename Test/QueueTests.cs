using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test
{
    /// <summary>
    /// Tests of BotL's Queue data type and its associated functions and primops
    /// </summary>
    [TestClass]
    public class QueueTests : BotLTestClass
    {
        [TestMethod]
        public void MakeQueue()
        {
            TestTrue("X=length(queue()), X=0");
        }

        [TestMethod]
        public void MakeEnqueueDequeue()
        {
            TestTrue("Q=queue(), enqueue(Q,1), enqueue(Q, 2), E1=dequeue(Q), E1=1, E2=dequeue(Q), E2=2, length(Q)=0");
        }

        [TestMethod, ExpectedException(typeof(InvalidOperationException))]
        public void DequeueEmptyQueue()
        {
            TestTrue("Q=queue(), E=dequeue(Q)");
        }

        [TestMethod]
        public void MatchQueueSucceed()
        {
            TestTrue("Q=queue(1,2,3), word(1, Q), word(2, Q), word(X, Q), X=3, length(Q)=0");
        }

        [TestMethod]
        public void MatchQueueFail()
        {
            TestFalse("Q=queue(1,2,3), word(1, Q), word(2, Q), word(X, Q), X=1");
        }

        [TestMethod]
        public void MatchQueueBacktrack()
        {
            TestTrue("Q=queue(1,2,3), ((word(1, Q), word(2, Q), word(0, Q))|(word(1, Q), word(2, Q), word(3, Q)))");
        }
    }
}
