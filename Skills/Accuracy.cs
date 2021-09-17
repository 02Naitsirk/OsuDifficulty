using System;
using MathNet.Numerics;

namespace OsuDifficulty.Skills
{
    public static class Accuracy
    {
        public static double CalculateAccuracyPerformance(Beatmap beatmap, double overallDifficulty, double clockRate,
            int count100, int count50, int countMiss)
        {
            const double scaling = 9000;

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
            double goodHitWindow = (139.5 - 8 * overallDifficulty) / clockRate;
            double mehHitWindow = (199.5 - 10 * overallDifficulty) / clockRate;

            double NegativeLogLikelihood(double x)
            {
                double greatProbability = SpecialFunctions.Erf(greatHitWindow / (Math.Sqrt(2) * x));
                double goodProbability = SpecialFunctions.Erfc(greatHitWindow / (Math.Sqrt(2) * x)) -
                                         SpecialFunctions.Erfc(goodHitWindow / (Math.Sqrt(2) * x));
                double mehProbability = SpecialFunctions.Erfc(goodHitWindow / (Math.Sqrt(2) * x)) -
                                        SpecialFunctions.Erfc(mehHitWindow / (Math.Sqrt(2) * x));

                double logLikelihood = (count300 + 1) * Math.Log(greatProbability) + (count100 + 1) * Math.Log(goodProbability) +
                       (count50 + 1) * Math.Log(mehProbability);
                
                return -logLikelihood;
            }

            double deviation = FindMinimum.OfScalarFunction(NegativeLogLikelihood, initialGuess);
            return deviation;
        }
    }
}