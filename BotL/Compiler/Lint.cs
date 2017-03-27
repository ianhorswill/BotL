using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace BotL.Compiler
{
    internal static class Lint
    {
        public static void Check(TextWriter output)
        {
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
                if (p.IsUserDefined && !refs.ContainsKey(p) && !p.IsExternallyCalled)
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
