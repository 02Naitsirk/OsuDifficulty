using System;
using MathNet.Numerics;
using OsuDifficulty.Skills;

namespace OsuDifficulty
{
    public static class Difficulty
    {
        public static double CalculateTotalStarRating(double aimStarRating, double tapStarRating)
        {
            return Math.Cbrt(Math.Pow(aimStarRating, 3) + Math.Pow(tapStarRating, 3));
        }

        public static double CalculateTotalPerformance(double aimPerformance, double tapPerformance,
            double accPerformance)
        {
            return aimPerformance + tapPerformance + accPerformance;
        }

        public static double CalculateAimPerformance(double starRating)
        {
            return Math.Pow(starRating, 3);
        }

        public static double CalculateTapPerformance(double starRating, Beatmap beatmap, double overallDifficulty, double clockRate,
            int count100, int count50, int countMiss)
        {
            double deviation = Accuracy.CalculateDeviation(beatmap, overallDifficulty, clockRate, count100, count50, countMiss);
            double deviationScaling = SpecialFunctions.Erf(13 / (Math.Sqrt(2) * deviation));
            return deviationScaling * Math.Pow(starRating, 3);
        }
    }
}