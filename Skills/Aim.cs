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

        public static double CalculateStarRating(List<HitObject> hitObjects, double circleSize, double clockRate)
        {
            double skillLevel = CalculateSkillLevel(hitObjects, circleSize, clockRate);
            double starRating = Scaling * Math.Pow(skillLevel, StarRatingPower);
            return starRating;
        }

        private static double CalculateHitProbability(HitObject currentObject, HitObject lastObject,
            HitObject? secondLastObject, double circleSize, double clockRate, double skill)
        {
            // This can be set to 1 and give very similar results as if it were set to something very small. Don't know why.
            const double integralTolerance = 1;

            double deltaTime = (currentObject.Time - lastObject.Time) / clockRate;
            double radius = 54.4 - 4.48 * circleSize;

            if (skill == 0 || deltaTime == 0 || radius == 0)
                return 0;

            double horizontalShift = Math.Abs(currentObject.X - lastObject.X);
            double verticalShift = Math.Abs(currentObject.Y - lastObject.Y);

            if (horizontalShift == 0 && verticalShift == 0)
            {
                return 1;
            }

            double horizontalDeviation = Math.Sqrt(horizontalShift) / (deltaTime * skill);
            double verticalDeviation = Math.Sqrt(verticalShift) / (deltaTime * skill);

            if (secondLastObject != null)
            {
                double lastDeltaTime = (lastObject.Time - secondLastObject.Time) / clockRate;
                double deltaTimeRatio = Math.Max(lastDeltaTime / deltaTime, deltaTime / lastDeltaTime);

                double deltaTimeRatioMultiplier = 0.5 / deltaTimeRatio + 0.5;

                horizontalDeviation *= deltaTimeRatioMultiplier;
                verticalDeviation *= deltaTimeRatioMultiplier;
            }
            else
            {
                horizontalDeviation *= 0.7;
                verticalDeviation *= 0.7;
            }

            if (horizontalDeviation > 0 && verticalDeviation == 0)
            {
                return SpecialFunctions.Erf(radius / (Math.Sqrt(2) * horizontalDeviation));
            }

            if (verticalDeviation > 0 && horizontalDeviation == 0)
            {
                return SpecialFunctions.Erf(radius / (Math.Sqrt(2) * verticalDeviation));
            }

            double Integrand(double x)
            {
                return SpecialFunctions.Erf(Math.Sqrt((Math.Pow(radius, 2) - x * Math.Pow(verticalDeviation, 2)) /
                                                      (2 * Math.Pow(horizontalDeviation, 2)))) * Math.Exp(-x / 2) /
                       Math.Sqrt(x);
            }

            double integralResult = Integrate.OnClosedInterval(Integrand, 0, Math.Pow(radius / verticalDeviation, 2),
                integralTolerance);

            return integralResult / Math.Sqrt(2 * Math.PI);
        }

        private static double CalculateFcProbability(IReadOnlyList<HitObject> hitObjects, double circleSize,
            double clockRate, double skill)
        {
            double fcProbability = 1;

            for (var i = 1; i < hitObjects.Count; i++)
            {
                var currentObject = hitObjects[i];
                var lastObject = hitObjects[i - 1];
                var secondLastObject = i > 1 ? hitObjects[i - 2] : null;

                double hitProbability = CalculateHitProbability(currentObject, lastObject, secondLastObject, circleSize,
                    clockRate, skill);

                fcProbability *= hitProbability;
            }

            return fcProbability;
        }

        private static double CalculateSkillLevel(IReadOnlyList<HitObject> hitObjects, double circleSize,
            double clockRate)
        {
            double FcProbabilityMinusThreshold(double x)
            {
                const double threshold = 0.5;
                return CalculateFcProbability(hitObjects, circleSize, clockRate, x) - threshold;
            }

            double skillLevel = Brent.FindRootExpand(FcProbabilityMinusThreshold, 0, 0.1);
            return skillLevel;
        }
    }
}