#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Tokenizer.cs" company="Ian Horswill">
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
using System.IO;
using System.Linq;
using System.Text;
using BotL.Compiler;

namespace BotL.Parser
{
    class Tokenizer
    {
        public Tokenizer(TextReader t)
        {
            sourceText = t;
        }

        // ReSharper disable once InconsistentNaming
        public static readonly object EOFToken = "****EOF****";

        readonly StringBuilder token = new StringBuilder();
        private object ungotToken;

        private const string SingleCharTokens = "{}()[]|,;.$#@";

        // ReSharper disable once InconsistentNaming
        private const char EOFChar = (char)0xffff;

        public object GetToken()
        {
            if (ungotToken != null)
            {
                var result = ungotToken;
                ungotToken = null;
                return result;
            }

            token.Length = 0;
            retry:
            SwallowWhiteSpace();
            var first = Get();
            if (first == '/')
            {
                switch (Peek())
                {
                    case '/':
                        SwallowToEndOfLine();
                        goto retry;

                    case '*':
                        SwallowMultilineComment();
                        goto retry;
                }
            }
            if (first == '"')
                return ReadString();
            if (first == '\'')
                return ReadQuotedSymbol();
            token.Append(first);
            if (IsSingleCharToken(first))
                return Symbol.Intern(token.ToString());
            if (char.IsLetter(first) || first == '_')
                return ReadSymbol();
            if (char.IsDigit(first))
                return ReadNumber();
            if (IsOperatorChar(first))
                return ReadOperatorSymbol();
            if (first == EOFChar)
                return EOFToken;
            throw new SyntaxError("Invalid start character for token: "+first, first);
        }

        private void SwallowMultilineComment()
        {
            char c;
            do
            {
                while ((c =Get()) != '*' && c != EOFChar) { }
            } while ((c = Peek()) != '/' && c != EOFChar);
            Get();
        }

        private void SwallowToEndOfLine()
        {
            char c;
            while ((c = Get()) != '\n' && c != EOFChar) { }
            Unget(c);
        }

        private object ReadString()
        {
            char c;
            while ((c = Get()) != '"')
            {
                switch (c)
                {
                    case EOFChar:
                        throw new SyntaxError("End of input encountered inside of string literal.", token.ToString());

                    case '\\':
                        c = Get();
                        switch (c)
                        {
                            case 'n':
                                token.Append('\n');
                                break;

                            case 'r':
                                token.Append('\r');
                                break;

                            case 't':
                                token.Append('\t');
                                break;

                            default:
                                token.Append(c);
                                break;
                        }
                        break;

                    default:
                        token.Append(c);
                        break;
                }
            }
            return token.ToString();
        }

        public object PeekToken()
        {
            return ungotToken ?? (ungotToken = GetToken());
        }

        private static bool IsSingleCharToken(char first)
        {
            return SingleCharTokens.Contains(first);
        }

        private object ReadQuotedSymbol()
        {
            for (var c = Get(); c != '\''; c = Get())
                token.Append(c);
            return Symbol.Intern(token.ToString());
        }

        private object ReadSymbol()
        {
            char c;
            for (c = Get(); (char.IsLetterOrDigit(c) || c == '_' || c == '!');)
            {
                token.Append(c);
                c = Get();
            }
            Unget(c);
            var name = token.ToString();
            switch (name)
            {
                case "true":
                case "True":
                    return true;

                case "False":
                case "false":
                    return false;

                // This isn't elegant, but we return null as a symbol
                // and convert it to the null pointer inside of ReadPrimary.
                // this prevents the parser from getting confused when it tries
                // to check if the result of an operation like *as* is null.
                //case "null":
                //    return null;

                default:
                    return Symbol.Intern(name);
            }
        }

        private object ReadOperatorSymbol()
        {
            char c;
            for (c = Get(); (IsOperatorChar(c) && !IsSingleCharToken(c));)
            {
                token.Append(c);
                c = Get();
            }
            Unget(c);
            return Symbol.Intern(token.ToString());
        }

        private const string OperatorChars = "@#$%^&*:<>?/!+-=|~\\";
        private bool IsOperatorChar(char c)
        {
            return OperatorChars.Contains(c);
        }

        private object ReadNumber()
        {
            bool gotDecimal = false;
            char c;
            for (c = Get(); (char.IsDigit(c) || c == '.');)
            {
                token.Append(c);
                if (c == '.')
                    gotDecimal = true;
                c = Get();
            }
            Unget(c);
            var tokString = token.ToString();
            // ReSharper disable RedundantCast
            return gotDecimal ? (object)float.Parse(tokString) : (object)int.Parse(tokString);
            // ReSharper restore RedundantCast
        }

        void SwallowWhiteSpace()
        {
            char c;
            do
            {
                c = Get();
            } while (char.IsWhiteSpace(c));
            Unget(c);
        }
        #region Get/unget

        private readonly TextReader sourceText;
        private char ungot = (char)0;

        char Get()
        {
            if (ungot != 0)
            {
                var ret = ungot;
                ungot = (char)0;
                return ret;
            }
            return (char)sourceText.Read();
        }

        void Unget(char c)
        {
            ungot = c;
        }

        private char Peek()
        {
            if (ungot == 0)
                ungot = (char)sourceText.Read();
            return ungot;
        }
    }
   #endregion
}
