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

        [TestMethod,ExpectedException(typeof(ArgumentTypeException))]
        public void IntFunctionTypeTest()
        {
            Functions.DeclareFunction("int_function", (int x) => x + 1);
            TestTrue("X=int_function(937.5), X=938");
        }

        [TestMethod]
        public void Int2FunctionTest()
        {
            Functions.DeclareFunction("int2_function", (int x, int y) => x - y);
            TestTrue("X=int2_function(937, 930), X=7");
        }

        [TestMethod]
        public void FloatFunctionTest()
        {
            Functions.DeclareFunction("float_function", (float x) => x + 1);
            TestTrue("X=float_function(937.0), X=938.0");
            TestTrue("X=float_function(937), X=938.0");
        }

        [TestMethod, ExpectedException(typeof(ArgumentTypeException))]
        public void FloatFunctionTypeTest()
        {
            Functions.DeclareFunction("float_function", (float x) => x + 1);
            TestTrue("X=float_function(a), X=938.0");
        }

        [TestMethod]
        public void ObjFunctionTest()
        {
            Functions.DeclareFunction("obj_function", (string x) => x);
            TestTrue("X=obj_function(\"foo\"), X=\"foo\"");
        }

        [TestMethod, ExpectedException(typeof(ArgumentTypeException))]
        public void ObjFunctionTestTypeTest()
        {
            Functions.DeclareFunction("string_function", (string x) => x);
            TestTrue("X=string_function(4)");
        }

        [TestMethod, ExpectedException(typeof(ArgumentTypeException))]
        public void ObjFunctionTestSubTypeTest()
        {
            Functions.DeclareFunction("string_function", (string x) => x);
            TestTrue("X=string_function($Array)");
        }
    }
}
