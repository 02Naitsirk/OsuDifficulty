using System;
using System.Collections.Generic;

namespace OsuDifficulty.Skills
{
    public static class Tap
    {
        private const double Scaling = 14;
        private const double StrainDecay = 1 / Math.E;

        public static double CalculateStarRating(Beatmap beatmap, double overallDifficulty, double clockRate)
        {
            var hitObjects = beatmap.HitObjects;
            double tapDifficulty = CalculateTapDifficulty(hitObjects, overallDifficulty, clockRate);
            return Scaling * Math.Sqrt(tapDifficulty);
        }

        private static double CalculateTapDifficulty(IReadOnlyList<HitObject> hitObjects, double overallDifficulty,
            double clockRate)
        {
            double mehHitWindow = (199.5 - 10 * overallDifficulty) / clockRate;
            double strain = 0;
            double maxStrain = strain;

            for (var i = 1; i < hitObjects.Count; i++)
            {
                var currentObject = hitObjects[i];
                var lastObject = hitObjects[i - 1];
                double deltaTime = (currentObject.Time - lastObject.Time) / clockRate;

                double extraTime = 0;

                if (i < hitObjects.Count - 1)
                {
                    var nextNote = hitObjects[i + 1];
                    double nextDeltaTime = (nextNote.Time - currentObject.Time) / clockRate;

                    if (nextDeltaTime > deltaTime)
                    {
                        double timeDifference = nextDeltaTime - deltaTime;
                        extraTime += Math.Min(mehHitWindow, timeDifference);
                    }
                }
                else
                {
                    extraTime += mehHitWindow;
                }

                extraTime = Math.Min(extraTime, mehHitWindow);
                double effectiveDeltaTime = deltaTime + extraTime;

                strain += 1 / effectiveDeltaTime;
                strain *= Math.Pow(StrainDecay, effectiveDeltaTime / 1000);

                if (strain > maxStrain)
                    maxStrain = strain;
            }

            return maxStrain;
        }
    }
}