#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FunctionalExpressionTests.cs" company="Ian Horswill">
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

namespace Test
{
    /// <summary>
    /// Summary description for FunctionalExpressionTests
    /// </summary>
    [TestClass]
    public class FunctionalExpressionTests
    {
        public FunctionalExpressionTests()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void ToStringTest()
        {
            TestTrue("(1).ToString() = \"1\"");
        }

        [TestMethod]
        public void MethodCallSucceedFailTest()
        {
            TestTrue("\"foo\".StartsWith(\"f\")");
            TestFalse("\"foo\".StartsWith(\"x\")");
        }

        [TestMethod]
        public void MultiargumentMethodCallTest()
        {
            TestTrue("\"foobar\".Replace(\"bar\", \"foo\") = \"foofoo\"");
        }

        [TestMethod]
        public void NewExpressionTest()
        {
            TestTrue("X= new ArrayList(), X.Add(1)");
        }

        [TestMethod]
        [ExpectedException(typeof(InstantiationException))]
        public void InstantiationExceptionTest()
        {
            Engine.Run("X=Y, X=1+X");
        }

        [TestMethod]
        public void ReadArrayTest()
        {
            TestTrue("X=array(1,2,3), X[1]=2");
            TestTrue("X=array(1,2,3), X[I]=2, I=1");
        }

        [TestMethod]
        public void ArithTests()
        {
            TestTrue("1+1=2");
            TestTrue("1+1.0=2.0");

            TestTrue("1*1=1");
            TestTrue("1*1.0=1.0");

            TestTrue("1-1=0");
            TestTrue("1-1.0=0.0");

            TestTrue("1/1=1.0");
            TestTrue("1/1.0=1.0");

            TestTrue("X=2, -2 = -X");
            TestTrue("2=abs(-2)");
        }

        [TestMethod]
        public void MinMaxTests()
        {
            TestTrue("1=min(1,2)");
            TestTrue("2=max(1,2)");
        }

        [TestMethod]
        public void FloorCeilingTests()
        {
            TestTrue("2.0=floor(2.5)");
            TestTrue("3.0=ceiling(2.5)");
        }

        [TestMethod]
        public void SqrtTest()
        {
            TestTrue("2.0=sqrt(4)");
        }

        [TestMethod]
        public void LogExpTest()
        {
            TestTrue("0.0=log(1)");
            TestTrue("1.0=exp(0)");
            TestTrue("1.0=pow(10,0)");
        }

        [TestMethod]
        public void TrigTests()
        {
            TestTrue("0.0=sin(0.0)");
            TestTrue("1.0=cos(0.0)");
            TestTrue("1.0=atan(tan(1.0))");
            TestTrue("1.0=atan(tan(1.0),1)");
        }

        [TestMethod]
        public void FieldReferenceTest()
        {
            TestTrue("$Single.Name=\"Single\"");
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
