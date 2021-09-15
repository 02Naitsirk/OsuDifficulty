using System;
using System.Collections.Generic;

namespace OsuDifficulty.Skills
{
    public static class Tap
    {
        private const double Scaling = 14.5;
        private const double StrainDecay = 1 / Math.E;

        public static double CalculateStarRating(IReadOnlyList<HitObject> hitObjects, double overallDifficulty,
            double clockRate)
        {
            double tapDifficulty = CalculateTapDifficulty(hitObjects, overallDifficulty, clockRate);
            return Scaling * Math.Sqrt(tapDifficulty);
        }

        private static double CalculateTapDifficulty(IReadOnlyList<HitObject> hitObjects, double overallDifficulty,
            double clockRate)
        {
            double greatHitWindow = (79.5 - 6 * overallDifficulty) / clockRate;

            double strain = 0;
            double maxStrain = strain;

            for (var i = 1; i < hitObjects.Count; i++)
            {
                var currentObject = hitObjects[i];
                var lastObject = hitObjects[i - 1];
                double deltaTime = (currentObject.Time - lastObject.Time) / clockRate;

                strain += 1 / (deltaTime + greatHitWindow);
                strain *= Math.Pow(StrainDecay, deltaTime / 1000);

                if (strain > maxStrain)
                    maxStrain = strain;
            }

            return maxStrain;
        }
    }
}