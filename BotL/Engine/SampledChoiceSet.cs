using System;
using System.Collections.Generic;

namespace BotL
{
    internal class SampledChoiceSet
    {
        struct ScoredChoice
        {
            public float Score;
            public object Choice;
        }

        readonly List<ScoredChoice> choices = new List<ScoredChoice>();

        public void Add(object choice, float score)
        {
            choices.Add(new ScoredChoice { Score = score, Choice = choice });
        }

        public object Choose(int keptChoiceCount)
        {
            choices.Sort((a, b) =>
            {
                var delta = a.Score - b.Score;
                if (delta > 0)
                    return -1;
                if (delta < 0)
                    return 1;
                return 0;
            });

            var end = Math.Min(choices.Count, keptChoiceCount);
            var total = 0f;
            for (int i = 0; i < end; i++)
                total += choices[i].Score;
            var choice = (float)(FunctionalExpression.Random.NextDouble() * total);
            var sum = 0f;
            for (int i = 0; i < end; i++)
            {
                sum += choices[i].Score;
                if (sum > choice)
                    return choices[i].Choice;
            }
            return choices[end - 1].Choice;
        }
    }
}
