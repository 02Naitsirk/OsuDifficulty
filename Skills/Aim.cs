#nullable enable
using System;
using System.Collections.Generic;
using MathNet.Numerics;
using MathNet.Numerics.RootFinding;

namespace OsuDifficulty.Skills
{
    public static class Aim
    {
        private const double Scaling = 325;
        private static readonly double StarRatingPower = Math.Log(1.4) / Math.Log(1.5);

        public static double CalculateStarRating(List<HitObject> hitObjects, double circleSize,
            double overallDifficulty, double clockRate, int missCount)
        {
            double skillLevel = CalculateSkillLevel(hitObjects, circleSize, overallDifficulty, clockRate, missCount);
            double starRating = Scaling * Math.Pow(skillLevel, StarRatingPower);
            return starRating;
        }

        private static double CalculateHitProbability(HitObject? nextObject, HitObject currentObject,
            HitObject lastObject, HitObject? secondLastObject, double circleSize, double overallDifficulty,
            double clockRate, double skill)
        {
            double deltaTime = (currentObject.Time - lastObject.Time) / clockRate;
            double radius = 54.4 - 4.48 * circleSize;
            double mehHitWindow = (199.5 - 10 * overallDifficulty) / clockRate;

            if (skill == 0 || deltaTime == 0 || radius == 0)
                return 0;

            // Orientation correction:
            // Sets the previous HitObject to (0, 0) and sets the current HitObject to (x, 0) where x is a real number.

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

            // Add extra time to deltaTime for cheesing corrections.
            double extraDeltaTime = 0;

            // Cheesing correction #1:
            // The player can tap the previous note early if the previous deltaTime is greater than the current deltaTime.
            // This kind of cheesing gives the player extra time to hit the current pattern.
            // The maximum amount of extra time is the 50 hit window or the time difference, whichever is lower.

            if (secondLastObject == null)
            {
                extraDeltaTime += mehHitWindow;
            }
            else
            {
                double previousDeltaTime = (lastObject.Time - secondLastObject.Time) / clockRate;
                double timeDifference = previousDeltaTime - deltaTime;
                if (timeDifference > 0)
                {
                    extraDeltaTime += Math.Min(mehHitWindow, timeDifference);
                }
            }

            // Cheesing correction #2:
            // The player can tap the current note late if the next deltaTime is greater than the current deltaTime.
            // This kind of cheesing gives the player extra time to hit the current pattern.
            // The maximum amount of extra time is the 50 hit window or the time difference, whichever is lower.

            if (nextObject == null)
            {
                extraDeltaTime += mehHitWindow;
            }
            else
            {
                double nextDeltaTime = (nextObject.Time - currentObject.Time) / clockRate;
                double timeDifference = nextDeltaTime - deltaTime;
                if (timeDifference > 0)
                {
                    extraDeltaTime += Math.Min(mehHitWindow, timeDifference);
                }
            }

            // Maximum amount of extra time is limited to the 50 hit window.
            extraDeltaTime = Math.Min(mehHitWindow, extraDeltaTime);

            double effectiveDeltaTime = deltaTime + extraDeltaTime;

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
            double overallDifficulty, double clockRate, double skill)
        {
            double expectedHits = 1;
            for (var i = 1; i < hitObjects.Count; i++)
            {
                var nextObject = i < hitObjects.Count - 1 ? hitObjects[i + 1] : null;
                var currentObject = hitObjects[i];
                var lastObject = hitObjects[i - 1];
                var secondLastObject = i > 1 ? hitObjects[i - 2] : null;

                double hitProbability = CalculateHitProbability(nextObject, currentObject, lastObject, secondLastObject,
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
                int threshold = 1 + missCount;
                double expectedHits = CalculateExpectedHits(hitObjects, circleSize, overallDifficulty, clockRate, skill);
                return hitObjects.Count - expectedHits - threshold;
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