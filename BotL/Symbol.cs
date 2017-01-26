#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Symbol.cs" company="Ian Horswill">
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
using System.Collections.Generic;

namespace BotL
{
    public sealed class Symbol
    {
        public readonly string Name;

        private static readonly Dictionary<string, Symbol> SymbolTable = new Dictionary<string, Symbol>();

        public static readonly Symbol Plus = Intern("+");
        public static readonly Symbol Minus = Intern("-");
        public static readonly Symbol Comma = Intern(",");
        public static readonly Symbol Slash = Intern("/");
        public static readonly Symbol Cut = Intern("!");
        public static readonly Symbol Disjunction = Intern("|");
        public static readonly Symbol Equal = Intern("=");
        public static readonly Symbol PlusEqual = Intern("+=");
        public static readonly Symbol Implication = Intern("<--");
        public static readonly Symbol Fail = Intern("fail");
        public static readonly Symbol TruePredicate = Intern("true");
        public static readonly Symbol Dot = Intern(".");
        public static readonly Symbol DollarSign = Intern("$");
        public static readonly Symbol Array = Intern("array");
        public static readonly Symbol New = Intern("new");
        public static readonly Symbol ArrayList = Intern("arraylist");
        public static readonly Symbol Hashset = Intern("hashset");
        public static readonly Symbol Colon = Intern(":");
        public static readonly Symbol Adjoin = Intern("adjoin");
        public static readonly Symbol Item = Intern("item");
        public static readonly Symbol Call = Intern("call");
        public static readonly Symbol Underscore = Intern("_");
        public static readonly Symbol Null = Intern("null");
        public static readonly Symbol ColonColon = Intern("::");


        Symbol(string n)
        {
            Name = n;
        }

        public static Symbol Intern(string name)
        {
            Symbol result;
            if (SymbolTable.TryGetValue(name, out result))
            {
                return result;
            }
            result = new Symbol(name);
            SymbolTable[name] = result;
            return result;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
