#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Lint.cs" company="Ian Horswill">
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
using System.IO;

namespace BotL.Compiler
{
    internal static class Lint
    {
        public static void Check(TextWriter output)
        {
            KB.Predicate(Symbol.Intern("top_level_goal"), 0).IsExternallyCalled = true;
            var refs = AllRulePredicateReferences();
            WarnUndefined(output, refs);
            WarnUnreferenced(output, refs);
            PrintClauseWarnings(output);
        }

        private static void PrintClauseWarnings(TextWriter output)
        {
            foreach (var p in KB.AllRulePredicates)
                if (p.IsUserDefined)
                    foreach (var c in p.Clauses)
                        foreach (var w in c.Warnings)
                            Warn(output, "In rule {0}: {1}", c.Source, w);
        }

        private static void WarnUnreferenced(TextWriter output, Dictionary<Predicate, List<Predicate>> refs)
        {
            foreach (var p in KB.AllRulePredicates)
                if (p.IsUserDefined && !refs.ContainsKey(p) && !p.IsExternallyCalled && !p.IsLocked && p.FirstClause != null)
                    Warn(output, "unused predicate {0}", p);
        }

        private static void WarnUndefined(TextWriter output, Dictionary<Predicate, List<Predicate>> refs)
        {
            foreach (var pair in refs)
            {
                var predicate = pair.Key;
                var referrers = pair.Value;
                if (!predicate.IsDefined)
                    foreach (var referrer in referrers)
                        Warn(output, "undefined predicate {0} referenced by {1}", predicate, referrer);
            }
        }

        private static void Warn(TextWriter output, string format, params object[] args)
        {
            output.Write("Warning: ");
            output.WriteLine(format, args);
        }

        static Dictionary<Predicate, List<Predicate>> AllRulePredicateReferences()
        {
            var result = new Dictionary<Predicate, List<Predicate>>();

            void AddReference(Predicate referrer, Predicate referee)
            {
                // ReSharper disable once CollectionNeverQueried.Local
                if (result.TryGetValue(referee, out List<Predicate> referers))
                    referers.Add(referrer);
                else
                    result[referee] = new List<Predicate> {referrer};
            }

            foreach (var rp in KB.AllRulePredicates)
            {
                foreach (var p in rp.ReferencedUserPredicates)
                    AddReference(rp, p);
            }
            return result;
        }
    }
}
