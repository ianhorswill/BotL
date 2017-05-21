using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace BotL
{
    /// <summary>
    /// A sampling-based profiler of BotL code.
    /// Samples the running predicates every SampleInterval VM instructions
    /// and builds a call tree with tick values
    /// </summary>
    public static class Profiler
    {
#if BotLProfiler
        /// <summary>
        /// Root of the call tree
        /// </summary>
        internal static ProfileNode CallTreeRoot = new ProfileNode(null);
        private static readonly Stack<ushort> continuationStack = new Stack<ushort>();
        private static ushort timeSinceSample;
        public static ushort DefaultSampleInterval = 100;
        public static int SampleInterval = Int32.MaxValue;   // Anything bigger than ushort.MaxValue means disabled.
        public static int TotalSamples;
#endif

        /// <summary>
        /// Starts the profiler running and resets the call tree.
        /// </summary>
        [Conditional("BotLProfiler")]
        public static void EnableProfiling()
        {
            SampleInterval = DefaultSampleInterval;
            CallTreeRoot = new ProfileNode(null);
            TotalSamples = 0;
        }

        /// <summary>
        /// Stops the profiler.
        /// </summary>
        [Conditional("BotLProfiler")]
        public static void DisableProfiling()
        {
            SampleInterval = Int32.MaxValue;
        }

        /// <summary>
        /// Sample the stack if the time is right.
        /// </summary>
        /// <param name="goalFrame">EnvironmentStack position of the goal frame.</param>
        [Conditional("BotLProfiler")]
        internal static void MaybeSampleStack(ushort goalFrame)
        {
#if BotLProfiler
            if (++timeSinceSample >= SampleInterval)
            {
                SampleStack(goalFrame);
                timeSinceSample = 0;
            }
#endif
        }

        /// <summary>
        /// Sample the stack if the time is right.
        /// </summary>
        /// <param name="goalFrame">EnvironmentStack position of the goal frame.</param>
        /// <param name="currentPrimitive">The primitive predicate that goalFrame is currently calling.</param>
        [Conditional("BotLProfiler")]
        internal static void MaybeSampleStack(ushort goalFrame, Predicate currentPrimitive)
        {
#if BotLProfiler
            if (++timeSinceSample >= SampleInterval)
            {
                SampleStack(goalFrame, currentPrimitive);
                timeSinceSample = 0;
            }
#endif
        }

#if BotLProfiler
        private static void SampleStack(ushort goalFrane, Predicate currentPrimitive)
        {
            var node = SampleStack(goalFrane);
            node.ExclusiveTicks--;
            var child = node.GetChild(currentPrimitive);
            child.InclusiveTicks++;
            child.ExclusiveTicks++;
        }

        /// <summary>
        /// Sample the running frames on the stack
        /// </summary>
        /// <param name="goalFrame"></param>
        private static ProfileNode SampleStack(ushort goalFrame)
        {
            TotalSamples++;
            // We aren't looking at all the frames in the environment stack, since it includes frames
            // that have succeeded.  So we start by following continuation links from goalFrame
            // back to the top of the stack to get just the running frames.
            Debug.Assert(continuationStack.Count == 0, "Internal stack non-empty on entry to SniffStack");
            for (var f = goalFrame; f > 0; f = Engine.EnvironmentStack[f].ContinuationFrame)
                if (!Engine.EnvironmentStack[f].Predicate.IsNestedPredicate)
                    continuationStack.Push(f);
            continuationStack.Push(0);
            // Now iterate through the running frames, incrementing their counters in the call tree.
            var node = CallTreeRoot;
            while (continuationStack.Count > 0)
            {
                var p = Engine.EnvironmentStack[continuationStack.Pop()].Predicate;
                node = node.GetChild(p);
                node.InclusiveTicks++;
            }
            // node is now the node for goalFrame
            node.ExclusiveTicks++;
            return node;
        }

        public class ProfileNode
        {
            public readonly Predicate Predicate;
            public int InclusiveTicks;
            public int ExclusiveTicks;
            public readonly List<ProfileNode> Children = new List<ProfileNode>();

            public ProfileNode(Predicate childPredicate)
            {
                Predicate = childPredicate;
            }

            public ProfileNode GetChild(Predicate childPredicate)
            {
                // This is predicated on the assumption that nodes have relatively few children
                // and so a brute-force search is more efficient than a hash table.  We shall
                // see if that's true in practice.
                foreach (var c in Children)
                {
                    if (c.Predicate == childPredicate)
                        return c;
                }

                var child = new ProfileNode(childPredicate);
                Children.Add(child);
                return child;
            }

            public override string ToString()
            {
                if (TotalSamples == 0)
                    return Predicate.Name.ToString();
                var elipsis = Children.Count > 0 ? "..." : "";
                return string.Format("{0}: {1:#.###%} {2:#.###%} {3}", Predicate, (float)InclusiveTicks / TotalSamples,
                   (float)ExclusiveTicks / TotalSamples, elipsis);
            }
            
            public void SortChildren()
            {
                Children.Sort((x, y) => y.InclusiveTicks - x.InclusiveTicks);
            }
        }
#endif
    }
}
