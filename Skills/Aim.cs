#nullable enable
using System;
using System.Collections.Generic;
using MathNet.Numerics;
using MathNet.Numerics.RootFinding;

namespace OsuDifficulty.Skills
{
    public static class Aim
    {
        private const double Scaling = 24;
        private static readonly double StarRatingPower = Math.Log(1.4) / Math.Log(1.5);

        public static double CalculateStarRating(Beatmap beatmap, double circleSize,
            double overallDifficulty, double clockRate, int missCount)
        {
            var hitObjects = beatmap.HitObjects;
            double skillLevel = CalculateAimDifficulty(hitObjects, circleSize, overallDifficulty, clockRate, missCount);
            double starRating = Scaling * Math.Pow(skillLevel, StarRatingPower);
            return starRating;
        }

        private static double CalculateHitProbability(HitObject? nextObject, HitObject currentObject,
            HitObject lastObject, HitObject? secondLastObject, double circleSize, double overallDifficulty,
            double clockRate, double skill)
        {
            double xDeviation;

            double deltaTime = (currentObject.Time - lastObject.Time) / clockRate;
            double radius = 54.4 - 4.48 * circleSize;
            double mehHitWindow = (199.5 - 10 * overallDifficulty) / clockRate;

            if (skill == 0 || deltaTime == 0 || radius == 0)
                return 0;

            double distance = Math.Sqrt(Math.Pow(currentObject.X - lastObject.X, 2) +
                                        Math.Pow(currentObject.Y - lastObject.Y, 2));

            if (distance == 0)
                return 1;

            // Add extra time to deltaTime for cheesing corrections.
            double extraDeltaTime = 0;

            /*
             * Correction #1: Early taps.
             * The player can tap the current note early if the previous deltaTime is greater than the current deltaTime.
             * This kind of cheesing gives the player extra time to hit the current pattern.
             * The maximum amount of extra time is the 50 hit window or the time difference, whichever is lower.
             */

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

            /*
             * Correction #2: Late taps.
             * The player can tap the current note late if the next deltaTime is greater than the current deltaTime.
             * This kind of cheesing gives the player extra time to hit the current pattern.
             * The maximum amount of extra time is the 50 hit window or the time difference, whichever is lower.
             */

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

            extraDeltaTime = Math.Min(mehHitWindow, extraDeltaTime);
            double effectiveDeltaTime = deltaTime + extraDeltaTime;

            const double k = 100;

            if (distance >= 2 * radius)
            {
                xDeviation = (distance + k) / (skill * effectiveDeltaTime);
            }
            else
            {
                xDeviation = distance * (2 * radius + k) / (2 * radius * skill * effectiveDeltaTime);
            }

            /*
             * Correction #3: Rotation
             * Rotation tends to change not the x deviation, but the y deviation.
             * When the rotation is close to 0 or 90 degrees, the y deviation is close to 0.
             * When the rotation approaches 45 degrees, the y deviation is around 0.75 * the x deviation.
             */

            double rotation = Math.Atan((double) (currentObject.Y - lastObject.Y) / (currentObject.X - lastObject.X));
            double rotationMultiplier = Math.Abs(Math.Sin(2 * rotation));
            double yDeviation = 0.75 * xDeviation * rotationMultiplier;

            /*
             * To compute the exact hit probability, a definite integral algorithm is required.
             * This definite algorithm is too slow for our needs, even with low precision.
             * So, we will approximate by multiplying two normal CDFs.
             * This has the effect of treating circles as squares, but it's a good, extremely fast approximation.
             */

            double xHitProbability = SpecialFunctions.Erf(radius / (Math.Sqrt(2) * xDeviation));
            double yHitProbability = yDeviation > 0 ? SpecialFunctions.Erf(radius / (Math.Sqrt(2) * yDeviation)) : 1;
            return xHitProbability * yHitProbability;
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

        private static double CalculateAimDifficulty(IReadOnlyList<HitObject> hitObjects, double circleSize,
            double overallDifficulty, double clockRate, int missCount)
        {
            const double guessLowerBound = 0;
            const double guessUpperBound = 0.1;

            double ExpectedHitsMinusThreshold(double skill)
            {
                int threshold = 1 + missCount;
                double expectedHits =
                    CalculateExpectedHits(hitObjects, circleSize, overallDifficulty, clockRate, skill);
                return hitObjects.Count - expectedHits - threshold;
            }

            try
            {
                double skillLevel =
                    Bisection.FindRootExpand(ExpectedHitsMinusThreshold, guessLowerBound, guessUpperBound);
                return skillLevel;
            }
            catch (NonConvergenceException e)
            {
                return double.PositiveInfinity;
            }
        }
    }
}