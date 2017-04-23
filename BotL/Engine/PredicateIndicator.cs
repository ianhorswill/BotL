#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PredicateIndicator.cs" company="Ian Horswill">
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
namespace BotL
{
    /// <summary>
    /// Specifies a name+arity for a predicate.  Used when looking up a predicate or indexing a table.
    /// </summary>
    internal struct PredicateIndicator
    {
        public PredicateIndicator(Symbol f, int a)
        {
            Functor = f;
            Arity = a;
        }

        public PredicateIndicator(string f, int a) : this(Symbol.Intern(f), a)
        { }

        public PredicateIndicator(object o)
        {
            if (o is Call c)
            {
                Functor = c.Functor;
                Arity = c.Arity;
            }
            else if (o is Symbol s)
            {
                Functor = s;
                Arity = 0;
            }
            else if (o is bool b)
            {
                Functor = b ? Symbol.TruePredicate : Symbol.Fail;
                Arity = 0;
            }
            else
                throw new ArgumentTypeException("PredicateIndicator", 0, "Expected a Symbol or Call", o);
        }

        public readonly Symbol Functor;
        public readonly int Arity;

        public override int GetHashCode()
        {
            return Functor.GetHashCode() ^ Arity.GetHashCode();
        }

        public static bool operator ==(PredicateIndicator a, PredicateIndicator b)
        {
            return a.Functor == b.Functor && a.Arity == b.Arity;
        }

        public static bool operator !=(PredicateIndicator a, PredicateIndicator b)
        {
            return a.Functor != b.Functor || a.Arity != b.Arity;
        }

        public override bool Equals(object obj)
        {
            if (obj is PredicateIndicator)
            {
                return this == (PredicateIndicator) obj;
            }
            return false;
        }

        public override string ToString()
        {
            return $"{Functor}/{Arity}";
        }
    }
}
