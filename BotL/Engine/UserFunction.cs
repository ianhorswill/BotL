using System;

namespace BotL
{
    public class UserFunction
    {
        internal static UserFunction[] UserFunctions = new UserFunction[255];
        private static int count;
        public readonly Symbol Name;
        public readonly int Arity;
        public readonly Func<ushort, ushort> Run;

        public UserFunction(Symbol name, int arity, Func<ushort, ushort> run)
        {
            Name = name;
            Arity = arity;
            UserFunctions[count++] = this;
            Run = run;
            FOpcodeTable.DefineUserFunction(name, arity);
        }

        public static byte Subopcode(Call call)
        {
            var pi = new PredicateIndicator(call);
            for (var i=0; i < count; i++)
            {
                var userFunction = UserFunctions[i];
                if (userFunction.Name == pi.Functor && userFunction.Arity == pi.Arity)
                    return (byte) i;
            }
            throw new InvalidOperationException("Call to undefined user function: "+call);
        }
    }
}
