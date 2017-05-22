using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BotL;

namespace Test
{
    [TestClass]
    public class UserFunctionTests : BotLTestClass
    {
        [TestMethod]
        public void IntFunctionTest()
        {
            Functions.DeclareFunction("int_function", (int x) => x+1);
            TestTrue("X=int_function(937), X=938");
        }

        [TestMethod]
        public void FloatFunctionTest()
        {
            Functions.DeclareFunction("float_function", (float x) => x + 1);
            TestTrue("X=float_function(937.0), X=938.0");
            TestTrue("X=float_function(937), X=938.0");
        }

        [TestMethod]
        public void ObjFunctionTest()
        {
            Functions.DeclareFunction("obj_function", (object x) => x.ToString());
            TestTrue("X=obj_function($Array), X=\"System.Array\"");
        }
    }
}
