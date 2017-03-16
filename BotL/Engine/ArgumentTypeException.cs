using System;

namespace BotL
{
    /// <summary>
    /// Specialized ArgumentException for when the call to a primop or function involved the wrong type.
    /// </summary>
    public class ArgumentTypeException : ArgumentException
    {
        /// <summary>
        /// Description of the problem
        /// </summary>
        private readonly string message;
        /// <summary>
        /// Name of the BotL primop or function that was called.
        /// </summary>
        private readonly string procName;
        /// <summary>
        /// Position in the argument list of the offending argument.  1=first argument.
        /// </summary>
        private readonly int argumentIndex;
        /// <summary>
        /// Value of the offending argument.
        /// </summary>
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
