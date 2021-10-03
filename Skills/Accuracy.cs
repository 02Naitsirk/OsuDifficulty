using System;
using MathNet.Numerics;

namespace OsuDifficulty.Skills
{
    public static class Accuracy
    {
        public static double CalculateAccuracyPerformance(Beatmap beatmap, double overallDifficulty, double clockRate,
            int count100, int count50, int countMiss)
        {
            const double scaling = 4750;

            if (beatmap.CircleCount == 0 || beatmap.ObjectCount == 0) return 0;

            double deviation = CalculateDeviation(beatmap, overallDifficulty, clockRate, count100, count50, countMiss);

            return scaling / Math.Pow(deviation, 2);
        }

        public static double CalculateDeviation(Beatmap beatmap, double overallDifficulty, double clockRate,
            int count100, int count50, int countMiss)
        {
            const double prior = 1;
            int count300 = beatmap.CircleCount - count100 - count50 - countMiss;
            double greatHitWindow = (79.5 - 6 * overallDifficulty) / clockRate;
            double deviation = greatHitWindow / (Math.Sqrt(2) *
                                                 SpecialFunctions.ErfInv((count300 + prior) /
                                                                         (beatmap.CircleCount + 2 * prior)));
            return deviation;
        }
    }
}