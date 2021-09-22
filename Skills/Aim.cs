#nullable enable
using System;
using System.Collections.Generic;
using MathNet.Numerics;
using MathNet.Numerics.RootFinding;

namespace OsuDifficulty.Skills
{
    public static class Aim
    {
        private const double Scaling = 295;
        private static readonly double StarRatingPower = Math.Log(1.4) / Math.Log(1.5);

        public static double CalculateStarRating(List<HitObject> hitObjects, double circleSize, double clockRate,
            int missCount)
        {
            double skillLevel = CalculateSkillLevel(hitObjects, circleSize, clockRate, missCount);
            double starRating = Scaling * Math.Pow(skillLevel, StarRatingPower);
            return starRating;
        }

        private static double CalculateHitProbability(HitObject currentObject, HitObject lastObject,
            HitObject? secondLastObject, double circleSize, double clockRate, double skill)
        {
            double deltaTime = (currentObject.Time - lastObject.Time) / clockRate;
            double radius = 54.4 - 4.48 * circleSize;

            if (skill == 0 || deltaTime == 0 || radius == 0)
                return 0;

            int horizontalShift = Math.Abs(currentObject.X - lastObject.X);
            int verticalShift = Math.Abs(currentObject.Y - lastObject.Y);

            if (horizontalShift == 0 && verticalShift == 0)
            {
                return 1;
            }

            double xDeviation = Math.Sqrt(horizontalShift) / (deltaTime * skill);
            double yDeviation = Math.Sqrt(verticalShift) / (deltaTime * skill);

            if (secondLastObject != null)
            {
                double lastDeltaTime = (lastObject.Time - secondLastObject.Time) / clockRate;
                double deltaTimeRatio = Math.Max(lastDeltaTime / deltaTime, deltaTime / lastDeltaTime);

                double deltaTimeRatioMultiplier = 0.5 / deltaTimeRatio + 0.5;

                xDeviation *= deltaTimeRatioMultiplier;
                yDeviation *= deltaTimeRatioMultiplier;
            }
            else
            {
                xDeviation *= 0.7;
                yDeviation *= 0.7;
            }

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
        /// Finds the probability of obtaining at most <paramref name="missCount"/> misses given a skill level of <paramref name="skill"></paramref>
        /// </summary>
        private static double CalculateProbability(IReadOnlyList<HitObject> hitObjects, double circleSize,
            double clockRate, double skill, int missCount)
        {
            if (missCount == 0)
            {
                double fcProbability = 1;

                for (var i = 1; i < hitObjects.Count; i++)
                {
                    var currentObject = hitObjects[i];
                    var lastObject = hitObjects[i - 1];
                    var secondLastObject = i > 1 ? hitObjects[i - 2] : null;

                    double hitProbability = CalculateHitProbability(currentObject, lastObject, secondLastObject,
                        circleSize,
                        clockRate, skill);

                    fcProbability *= hitProbability;
                }

                return fcProbability;
            }

            // Use Poisson distribution to approximate a Poisson binomial distribution

            double mean = 0;
            for (var i = 1; i < hitObjects.Count; i++)
            {
                var currentObject = hitObjects[i];
                var lastObject = hitObjects[i - 1];
                var secondLastObject = i > 1 ? hitObjects[i - 2] : null;

                double hitProbability = CalculateHitProbability(currentObject, lastObject, secondLastObject, circleSize,
                    clockRate, skill);

                mean += 1 - hitProbability;
            }

            double probability = 0;
            for (var i = 0; i <= missCount; i++)
            {
                probability += Math.Pow(mean, i) * Math.Exp(-mean) / SpecialFunctions.Factorial(i);
            }

            return probability;
        }

        private static double CalculateSkillLevel(IReadOnlyList<HitObject> hitObjects, double circleSize,
            double clockRate, int missCount)
        {
            const double guessLowerBound = 0;
            const double guessUpperBound = 0.1;

            double ProbabilityMinusThreshold(double skill)
            {
                const double threshold = 0.5;
                return CalculateProbability(hitObjects, circleSize, clockRate, skill, missCount) - threshold;
            }

            double skillLevel = Brent.FindRootExpand(ProbabilityMinusThreshold, guessLowerBound, guessUpperBound);
            return skillLevel;
        }
    }
}