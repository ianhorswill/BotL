#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="QueueTests.cs" company="Ian Horswill">
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
