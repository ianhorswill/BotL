#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Repl.cs" company="Ian Horswill">
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
using System.IO;
using BotL.Parser;
using BotL.Unity;

namespace BotL
{
    public static class Repl
    {
        public static TextWriter StandardOutput;
        public static TextWriter StandardError;
        public static TextReader StandardInput;

        /// <summary>
        /// True if we're running outside of Unity.
        /// </summary>
        public static bool IsStandalone;

        public static void ReadEvalPrintLoop(TextReader input, TextWriter output, TextWriter error)
        {
            StandardInput = input;
            StandardOutput = output;
            StandardError = error;
            while (true)
            {
                Console.Write("> ");
                var command = StandardInput.ReadLine();
                GlobalVariable.Time.Value.Set(System.Environment.TickCount);
                if (RunCommand(command)) return;
                StandardOutput.Flush();
            }
            // ReSharper disable once FunctionNeverReturns
        }

        public static bool RunCommand(string command)
        {
            if (!IsStandalone)
                UnityUtilities.SetUnityGlobals(null, null);
            switch (command)
            {
                case "quit":
                    return true;

#if DEBUG
                case "debug":
                    Engine.SingleStep = true;
                    break;
#endif

                default:
                    // ReSharper disable once PossibleNullReferenceException
                    if (command.StartsWith("load "))
                    {
                        try
                        {
                            Compiler.Compiler.CompileFile(command.Substring(5));
                            StandardOutput.WriteLine("Compiled.");
                        }
                        catch (Exception e)
                        {
                            StandardError.WriteLine(e.Message);
                        }
                    }
                    else if (command.StartsWith("assert "))
                    {
                        try
                        {
                            Compiler.Compiler.Compile(command.Substring("assert ".Length));
                            StandardOutput.WriteLine("Compiled");
                        }
                        catch (Exception e)
                        {
                            StandardError.WriteLine(e.Message);
                        }
                    }
                    else if (command.StartsWith("table ") || command.StartsWith("function ")
                             || command.StartsWith("trace ") || command.StartsWith("notrace ") 
                             || command.StartsWith("global "))
                    {
                        try
                        {
                            Compiler.Compiler.Compile(command);
                        }
                        catch (Exception e)
                        {
                            StandardError.WriteLine(e.Message);
                        }
                    }
                    else
                    {
                        var success = false;
                        var completed = false;
#if DEBUG
                        if (Engine.SingleStep)
                        {
                            success = Engine.Run(command);
                            completed = true;
                        }
                        else
#endif
                        {
                            try
                            {
                                success = Engine.Run(command);
                                completed = true;
                            }
                            catch (Exception e)
                            {
                                StandardError.WriteLine(e.Message);
                            }
                        }
                        StandardOutput.WriteLine(success);
                        if (completed && success)
                        {
                            foreach (var b in Engine.TopLevelResultBindings)
                            {
                                StandardOutput.WriteLine($"{b.Key} = {ExpressionParser.WriteExpressionToString(b.Value)}");
                            }
                        }
                    }
                    break;
            }
            return false;
        }
    }
}
