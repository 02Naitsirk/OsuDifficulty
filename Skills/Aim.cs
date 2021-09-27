#nullable enable
using System;
using System.Collections.Generic;
using MathNet.Numerics;
using MathNet.Numerics.RootFinding;

namespace OsuDifficulty.Skills
{
    public static class Aim
    {
        private const double Scaling = 310;
        private static readonly double StarRatingPower = Math.Log(1.4) / Math.Log(1.5);

        public static double CalculateStarRating(List<HitObject> hitObjects, double circleSize,
            double overallDifficulty, double clockRate, int missCount)
        {
            double skillLevel = CalculateSkillLevel(hitObjects, circleSize, overallDifficulty, clockRate, missCount);
            double starRating = Scaling * Math.Pow(skillLevel, StarRatingPower);
            return starRating;
        }

        private static double CalculateHitProbability(HitObject currentObject, HitObject lastObject,
            HitObject? secondLastObject, double circleSize, double overallDifficulty, double clockRate, double skill)
        {
            double deltaTime = (currentObject.Time - lastObject.Time) / clockRate;
            double radius = 54.4 - 4.48 * circleSize;
            double mehHitWindow = (199.5 - 10 * overallDifficulty) / clockRate;

            if (skill == 0 || deltaTime == 0 || radius == 0)
                return 0;

            // Orientation correction:
            // Sets the previous HitObject to (0, 0) and sets the current HitObject to (x, 0)
            // where x is a real number.

            int shiftedCurrentX = currentObject.X - lastObject.X;
            int shiftedCurrentY = currentObject.Y - lastObject.Y;

            // Stacks are assumed to have a 100% hit probability.

            if (shiftedCurrentX == 0 && shiftedCurrentY == 0)
                return 1;

            double slope = (double) shiftedCurrentY / shiftedCurrentX;
            double angle = -Math.Atan(slope);

            double rotatedCurrentX = shiftedCurrentX * Math.Cos(angle) - shiftedCurrentY * Math.Sin(angle);
            const double rotatedCurrentY = 0;

            double xShift = Math.Abs(rotatedCurrentX);
            double yShift = Math.Abs(rotatedCurrentY);

            // Use effective delta time for cheesing corrections.

            double effectiveDeltaTime = deltaTime;

            // Cheesing correction #1:
            // The player can tap a note early if the previous deltaTime is greater than the current deltaTime.
            // The maximum amount of extra time is the 50 hit window or the time difference, whichever is lower.

            if (secondLastObject == null)
            {
                effectiveDeltaTime += mehHitWindow;
            }
            else
            {
                double previousDeltaTime = (lastObject.Time - secondLastObject.Time) / clockRate;
                double timeDifference = previousDeltaTime - deltaTime;
                if (timeDifference > 0)
                {
                    effectiveDeltaTime += Math.Min(mehHitWindow, timeDifference);
                }
            }

            double xDeviation = Math.Sqrt(xShift) / (effectiveDeltaTime * skill);
            double yDeviation = Math.Sqrt(yShift) / (effectiveDeltaTime * skill);

            if (xDeviation > 0 && yDeviation == 0)
            {
                return SpecialFunctions.Erf(radius / (Math.Sqrt(2) * xDeviation));
            }

            if (yDeviation > 0 && xDeviation == 0)
            {
                return SpecialFunctions.Erf(radius / (Math.Sqrt(2) * yDeviation));
            }

            double Integrand(double x)
            {
                return SpecialFunctions.Erf(1 / xDeviation * Math.Sqrt(0.5 * (radius * radius - x * x))) *
                       Math.Exp(-0.5 * x * x / (yDeviation * yDeviation));
            }

            const int order = 64;
            double constant = 2 / (yDeviation * Math.Sqrt(2 * Math.PI));

            return constant * Integrate.GaussLegendre(Integrand, 0, radius, order);
        }

        /// <summary>
        /// Finds the expected number of hits given a skill level of <paramref name="skill"/>.
        /// </summary>
        private static double CalculateExpectedHits(IReadOnlyList<HitObject> hitObjects, double circleSize,
            double overallDifficulty, double clockRate, double skill, int missCount)
        {
            double expectedHits = 1;
            for (var i = 1; i < hitObjects.Count; i++)
            {
                var currentObject = hitObjects[i];
                var lastObject = hitObjects[i - 1];
                var secondLastObject = i > 1 ? hitObjects[i - 2] : null;

                double hitProbability = CalculateHitProbability(currentObject, lastObject, secondLastObject,
                    circleSize, overallDifficulty, clockRate, skill);

                expectedHits += hitProbability;
            }

            return expectedHits;
        }

        private static double CalculateSkillLevel(IReadOnlyList<HitObject> hitObjects, double circleSize,
            double overallDifficulty, double clockRate, int missCount)
        {
            const double guessLowerBound = 0;
            const double guessUpperBound = 0.01;

            double ExpectedHitsMinusThreshold(double skill)
            {
                const double threshold = 1.0;
                return hitObjects.Count -
                       CalculateExpectedHits(hitObjects, circleSize, overallDifficulty, clockRate, skill, missCount) -
                       threshold - missCount;
            }

            try
            {
                double skillLevel = Brent.FindRootExpand(ExpectedHitsMinusThreshold, guessLowerBound, guessUpperBound);
                return skillLevel;
            }
            catch (NonConvergenceException e)
            {
                Console.WriteLine(e);
                return double.PositiveInfinity;
            }
        }
    }
}