using System;
using OsuDifficulty.Skills;

namespace OsuDifficulty
{
    internal static class Program
    {
        private static void Main()
        {
            const int goodCount = 0;
            const int mehCount = 0;
            const int missCount = 0;
            const double comboProportion = 1;

            Console.WriteLine("\nProgram started.\n");
            Beatmap beatmap = new();
            var os = Environment.OSVersion;
            while (true)
            {
                try
                {
                    string beatmapPath = os.Platform.ToString() == "Unix"
                        ? Console.ReadLine()?.Replace(@"\", string.Empty).Trim()
                        : Console.ReadLine()?.Replace("\"", "").Trim();

                    if (beatmapPath == "e")
                    {
                        Console.WriteLine("\nProgram closing.\n");
                        break;
                    }

                    beatmap.ParseBeatmapFile(beatmapPath);

                    var hitObjects = beatmap.HitObjects;
                    double ezCircleSize = beatmap.CircleSize / 2;
                    double nmCircleSize = beatmap.CircleSize;
                    double hrCircleSize = Math.Min(beatmap.CircleSize * 1.3, 10);

                    double[] circleSizes =
                    {
                        ezCircleSize, nmCircleSize, hrCircleSize
                    };

                    double ezApproachRate = beatmap.ApproachRate / 2;
                    double nmApproachRate = beatmap.ApproachRate;
                    double hrApproachRate = Math.Min(10, beatmap.ApproachRate * 1.4);

                    double[] approachRates =
                    {
                        ezApproachRate, nmApproachRate, hrApproachRate
                    };

                    double ezOverallDifficulty = beatmap.OverallDifficulty / 2;
                    double nmOverallDifficulty = beatmap.OverallDifficulty;
                    double hrOverallDifficulty = Math.Min(beatmap.OverallDifficulty * 1.4, 10);

                    double[] overallDifficulties =
                    {
                        ezOverallDifficulty, nmOverallDifficulty, hrOverallDifficulty
                    };

                    const double htClockRate = 0.75;
                    const double nmClockRate = 1.0;
                    const double dtClockRate = 1.5;

                    double[] clockRates =
                    {
                        htClockRate, nmClockRate, dtClockRate
                    };

                    string[] mods = {"HTEZ", "HT", "HTHR", "EZ", "NM", "HR", "DTEZ", "DT", "DTHR"};

                    int count300 = beatmap.ObjectCount - goodCount - mehCount - missCount;

                    var aimStarRatings = new double[9];
                    for (var i = 0; i < aimStarRatings.Length; i++)
                        aimStarRatings[i] = Aim.CalculateStarRating(hitObjects, circleSizes[i % 3], clockRates[i / 3]);

                    var tapStarRatings = new double[9];
                    for (var i = 0; i < tapStarRatings.Length; i++)
                        tapStarRatings[i] =
                            Tap.CalculateStarRating(hitObjects, overallDifficulties[i % 3], clockRates[i / 3]);

                    var starRatings = new double[9];
                    for (var i = 0; i < starRatings.Length; i++)
                        starRatings[i] = Difficulty.CalculateTotalStarRating(aimStarRatings[i], tapStarRatings[i]);

                    var aimPerformanceValues = new double[9];
                    for (var i = 0; i < aimPerformanceValues.Length; i++)
                        aimPerformanceValues[i] =
                            Difficulty.CalculateAimPerformance(aimStarRatings[i]);

                    var tapPerformanceValues = new double[9];
                    for (var i = 0; i < tapPerformanceValues.Length; i++)
                        tapPerformanceValues[i] =
                            Difficulty.CalculateTapPerformance(tapStarRatings[i]);

                    var accPerformanceValues = new double[9];
                    for (var i = 0; i < accPerformanceValues.Length; i++)
                        accPerformanceValues[i] = Accuracy.CalculateAccuracyPerformance(beatmap,
                            overallDifficulties[i % 3],
                            clockRates[i / 3], goodCount, mehCount, missCount);

                    var performanceValues = new double[9];
                    for (var i = 0; i < performanceValues.Length; i++)
                        performanceValues[i] = Difficulty.CalculateTotalPerformance(aimPerformanceValues[i],
                            tapPerformanceValues[i], accPerformanceValues[i]);

                    const int s = -10;

                    Console.WriteLine($"\n{beatmap.Artist} - {beatmap.Title} ({beatmap.Creator}) [{beatmap.Version}]");
                    Console.WriteLine(
                        $"CS: {beatmap.CircleSize}, AR: {beatmap.ApproachRate}, OD: {beatmap.OverallDifficulty}\n");

                    Console.WriteLine(
                        $"Good: {goodCount}, Meh: {mehCount}, Miss: {missCount}, Combo (notes): {(int) (beatmap.ObjectCount * comboProportion)} / {beatmap.ObjectCount}");
                    Console.WriteLine(
                        $"Accuracy: {Math.Round(100 * (double) (300 * count300 + 100 * goodCount + 50 * mehCount) / (300 * beatmap.ObjectCount), 2)}%\n");

                    Console.WriteLine(
                        $"{"Mods",s}" +
                        $"{"Stars",s}" +
                        $"{"PP",s}" +
                        $"{"Aim SR",s}" +
                        $"{"Tap SR",s}" +
                        $"{"Aim PP",s}" +
                        $"{"Tap PP",s}" +
                        $"{"Acc PP",s}");

                    for (int i = 0; i < aimStarRatings.Length; i++)
                    {
                        if (i > 0 && i % 3 == 0) Console.WriteLine();

                        Console.WriteLine($"{mods[i],s}" +
                                          $"{Math.Round(starRatings[i], 2),s}" +
                                          $"{Math.Round(performanceValues[i]),s}" +
                                          $"{Math.Round(aimStarRatings[i], 2),s}" +
                                          $"{Math.Round(tapStarRatings[i], 2),s}" +
                                          $"{Math.Round(aimPerformanceValues[i]),s}" +
                                          $"{Math.Round(tapPerformanceValues[i]),s}" +
                                          $"{Math.Round(accPerformanceValues[i]),s}");
                        if (i == 8) Console.WriteLine();
                    }

                    beatmap.HitObjects.Clear();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\n{e}\n");
                }
            }
        }
    }
}