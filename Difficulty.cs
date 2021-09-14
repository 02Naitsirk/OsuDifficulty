using System;

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

        public static double CalculateTapPerformance(double starRating)
        {
            return Math.Pow(starRating, 3);
        }
    }
}