using System;
using MathNet.Numerics;

namespace OsuDifficulty.Skills
{
    public static class Accuracy
    {
        public static double CalculateAccuracyPerformance(Beatmap beatmap, double overallDifficulty, double clockRate,
            int count100, int count50, int countMiss)
        {
            const double scaling = 5000;

            if (beatmap.CircleCount == 0 || beatmap.ObjectCount == 0) return 0;

            double deviation = CalculateDeviation(beatmap, overallDifficulty, clockRate, count100, count50, countMiss);

            return scaling / Math.Pow(deviation, 2);
        }

        private static double CalculateDeviation(Beatmap beatmap, double overallDifficulty, double clockRate,
            int count100, int count50, int countMiss)
        {
            const double initialGuess = 8;

            int count300 = beatmap.CircleCount - count100 - count50 - countMiss;
            double greatHitWindow = (79.5 - 6 * overallDifficulty) / clockRate;

            double NegativeLogLikelihood(double x)
            {
                double likelihood =
                    (count300 + 1) * Math.Log(1 - SpecialFunctions.Erfc(greatHitWindow / (x * Math.Sqrt(2)))) +
                    (countMiss + 2 * count50 + count100 + 1) *
                    Math.Log(SpecialFunctions.Erfc(greatHitWindow / (x * Math.Sqrt(2))));

                return -likelihood;
            }

            double deviation = FindMinimum.OfScalarFunction(NegativeLogLikelihood, initialGuess);
            return deviation;
        }
    }
}