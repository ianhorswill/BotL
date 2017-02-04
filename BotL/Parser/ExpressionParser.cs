#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExpressionParser.cs" company="Ian Horswill">
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BotL.Compiler;

namespace BotL.Parser
{
    public class ExpressionParser
    {
        static ExpressionParser()
        {
            DefineBinaryOperator("<--", 10);

            DefineBinaryOperator(",", 20);

            DefineBinaryOperator("|", 21);

            DefineBinaryOperator("->", 22);

            DefinePrefixOperator("set", 25);
            DefinePrefixOperator("function", 25);
            DefinePrefixOperator("table", 25);
            DefinePrefixOperator("global", 25);
            DefinePrefixOperator("struct", 25);
            DefinePrefixOperator("signature", 25);
            DefinePrefixOperator("trace", 25);
            DefinePrefixOperator("notrace", 25);

            DefineBinaryOperator("=", 30);
            DefineBinaryOperator("+=", 30);

            DefineBinaryOperator("\\=", 30);
            DefineBinaryOperator("<", 30);
            DefineBinaryOperator("=<", 30);
            DefineBinaryOperator(">", 30);
            DefineBinaryOperator(">=", 30);

            DefineBinaryOperator("in", 30);

            DefinePrefixOperator("new", 50);

            DefineBinaryOperator("+", 110);
            DefineBinaryOperator("-", 110, 110);

            DefineBinaryOperator("*", 120);
            DefineBinaryOperator("/", 120, 130);
            DefineBinaryOperator("%", 120);
            DefineBinaryOperator(":", 120);
            DefineBinaryOperator(">>", 120);
            DefineBinaryOperator(".", 200);
            DefinePrefixOperator("$", 300);
            DefineBinaryOperator("::", 300);
        }

        private readonly Tokenizer tok;

        private static readonly Dictionary<Symbol, OperatorInfo> OperatorTable = new Dictionary<Symbol, OperatorInfo>();

        public ExpressionParser(TextReader t)
        {
            tok = new Tokenizer(t);
        }

        public ExpressionParser(string s) : this(new StringReader(s)) { }

        // ReSharper disable once InconsistentNaming
        public bool EOF => tok.PeekToken() == Tokenizer.EOFToken;

        public object Read(bool isArgument=false)
        {
            return ReadExpression(ReadPrimary(), 0, isArgument);
        }

        private object ReadExpression(object lhs, int minPrec, bool isArgument)
        {
            var lookAhead = tok.PeekToken() as Symbol;
            if (isArgument && lookAhead == Symbol.Comma)
                lookAhead = null;
            while (IsBinaryWithPrecedence(lookAhead, minPrec))
            {
                var op = lookAhead;
                tok.GetToken();
                // ReSharper disable once PossibleInvalidOperationException
                int opPrec = OperatorTable[op].BinaryPrecedence.Value; 
                var rhs = ReadPrimary();
                lookAhead = tok.PeekToken() as Symbol;
                if (isArgument && lookAhead == Symbol.Comma)
                    lookAhead = null;
                while (IsBinaryWithPrecedence(lookAhead, opPrec + 1))
                {
                    // ReSharper disable once PossibleInvalidOperationException
                    rhs = ReadExpression(rhs, OperatorTable[lookAhead].BinaryPrecedence.Value, false);
                    lookAhead = tok.PeekToken() as Symbol;
                }
                lhs = new Call(op, lhs, rhs);
            }
            return lhs;
        }

        private static readonly Symbol OpenParen = Symbol.Intern("(");
        private static readonly Symbol CloseParen = Symbol.Intern(")");
        private static readonly Symbol OpenBracket = Symbol.Intern("[");
        private static readonly Symbol CloseBracket = Symbol.Intern("]");

        private object ReadPrimary()
        {
            var t = tok.GetToken();
            if (t == Symbol.Null)
                return null;
            if (t == Symbol.Plus && IsNumber(tok.PeekToken()))
                return tok.GetToken();
            if (t == Symbol.Minus && IsNumber(tok.PeekToken()))
                return Negate(tok.GetToken());

            var symbol = t as Symbol;
            if (t == OpenParen)
                return ReadNestedExpression();
            if (symbol != null && tok.PeekToken() == OpenParen)
                return ReadCall(symbol);
            if (symbol != null && tok.PeekToken() == OpenBracket)
                return ReadArrayElementExpression(symbol);

            var op = Operator(symbol);
            if (op?.PrefixPrecedence != null)
                return new Call(symbol, ReadExpression(ReadPrimary(), op.PrefixPrecedence.Value, false));

            return t;
        }

        private object ReadNestedExpression()
        {
            var value = Read();
            var close = tok.GetToken();
            if (close != CloseParen)
                throw new SyntaxError($"Nested expression {value} does not have matching close paren; got {close} instead", value);
            return value;
        }

        private object ReadArrayElementExpression(Symbol symbol)
        {
            tok.GetToken(); // swallow bracket
            var elementExpression = Read();
            var closeToken = tok.GetToken();
            if (closeToken != CloseBracket)
                throw new SyntaxError(
                    $"Syntax error in array index expression.  Expected ], got {closeToken}", symbol);
            return new Call(Symbol.Item, symbol, elementExpression);
        }

        private object ReadCall(Symbol symbol)
        {
            tok.GetToken();
            var args = new List<object>();
            while (tok.PeekToken() != CloseParen)
            {
                args.Add(Read(true));
                var delimiter = tok.PeekToken();
                if (delimiter != CloseParen)
                {
                    if (delimiter == Symbol.Comma)
                        tok.GetToken();
                    else
                        throw new SyntaxError(
                            $"Syntax error in argument list to {symbol}.  Expected comma, got {tok.GetToken()}", symbol);
                }
            }
            tok.GetToken();
            return new Call(symbol, args.ToArray());
        }

        bool IsNumber(object o)
        {
            return o is int || o is float;
        }

        object Negate(object o)
        {
            if (o is int)
                return -(int) o;
            return -(float) o;
        }

        bool IsBinaryWithPrecedence(Symbol name, int minPrec)
        {
            if (name == null)
                return false;

            OperatorInfo op;
            if (OperatorTable.TryGetValue(name, out op))
            {
                return op.BinaryPrecedence.HasValue && op.BinaryPrecedence >= minPrec;
            }
            return false;
        }

        class OperatorInfo
        {
            private readonly Symbol name;
            public readonly int? BinaryPrecedence;
            public readonly int? PrefixPrecedence;
            // ReSharper disable once MemberCanBePrivate.Local
            // ReSharper disable once NotAccessedField.Local
            public readonly int? PostfixPrecedence;

            public OperatorInfo(Symbol name, int? binaryPrecedence, int? prefixPrecedence, int? postfixPrecedence)
            {
                this.name = name;
                BinaryPrecedence = binaryPrecedence;
                PrefixPrecedence = prefixPrecedence;
                PostfixPrecedence = postfixPrecedence;
            }

            public override string ToString()
            {
                return $"Operator<{name}>";
            }
        }

        static OperatorInfo Operator(Symbol name)
        {
            if (name == null)
                return null;
            OperatorInfo result;
            if (!OperatorTable.TryGetValue(name, out result))
                return null;
            return result;
        }

        static void DefineBinaryOperator(string name, int binaryPrec, int? prefixPrec = null)
        {
            var s = Symbol.Intern(name);
            OperatorTable[s] = new OperatorInfo(s, binaryPrec, prefixPrec, null);
        }

        static void DefinePrefixOperator(string name, int prec)
        {
            var s = Symbol.Intern(name);
            OperatorTable[s] = new OperatorInfo(s, null, prec, null);
        }

        public static string WriteExpressionToString(object expression)
        {
            StringBuilder b = new StringBuilder();

            WriteExpressionToString(expression, 0, b);
            return b.ToString();
        }

        private static void WriteExpressionToString(object expression, int minPrec, StringBuilder b)
        {
            if (expression == null)
            {
                b.Append("null");
                return;
            }
            if (expression == Structs.PaddingValue)
            {
                b.Append("<>");
                return;
            }
            var s = expression as string;
            if (s != null)
            {
                b.Append('"');
                b.Append(s);
                b.Append('"');
                return;
            }
            var l = expression as IList;
            if (l != null)
            {
                switch (l.GetType().Name)
                {
                    case "ArrayList":
                        b.Append("arraylist(");
                        break;

                    default:
                        if (l is object[])
                            b.Append("array(");
                        else
                            b.Append("list(");
                        break;
                }
                var first = true;
                foreach (var e in l)
                {
                    if (first)
                        first = false;
                    else
                    {
                        b.Append(", ");
                    }
                    WriteExpressionToString(e, 0, b);
                }
                b.Append(")");
                return;
            }
            var set = expression as HashSet<object>;
            if (set != null)
            {
                b.Append("hashset(");
                var first = true;
                foreach (var e in set)
                {
                    if (first)
                        first = false;
                    else
                    {
                        b.Append(", ");
                    }
                    WriteExpressionToString(e, 0, b);
                }
                b.Append(")");
                return;
            }
            var c = expression as Call;
            if (c == null)
            {
                b.Append(expression);
            }
            else
            {
                var op = c.Arity == 2 ? Operator(c.Functor) : null;
                if (op?.BinaryPrecedence != null)
                {
                    bool parenthesize = op.BinaryPrecedence < minPrec;
                    if (parenthesize)
                        b.Append('(');
                    WriteExpressionToString(c.Arguments[0], op.BinaryPrecedence.Value, b);
                    b.Append(c.Functor);
                    WriteExpressionToString(c.Arguments[1], op.BinaryPrecedence.Value, b);
                    if (parenthesize)
                        b.Append(')');
                }
                else
                {
                    b.Append(c.Functor);
                    b.Append('(');
                    var first = true;
                    foreach (var a in c.Arguments)
                    {
                        if (first)
                            first = false;
                        else
                            b.Append(", ");
                        WriteExpressionToString(a, 0, b);
                    }
                    b.Append(')');
                }
            }
        }
    }
}
