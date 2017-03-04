using System;

namespace BotL
{
    public class ArgumentTypeException : ArgumentException
    {
        private readonly string message;
        private readonly string procName;
        private readonly int argumentIndex;
        private readonly object argument;

        public ArgumentTypeException(string procName, int argumentIndex, string message, object argument)
        {
            this.procName = procName;
            this.argumentIndex = argumentIndex;
            this.message = message;
            this.argument = argument;
        }

        public override string Message => $"In {procName}, argument {argumentIndex}={argument}: {message}";
    }
}
