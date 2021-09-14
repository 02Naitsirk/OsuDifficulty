using System;
using System.Collections.Generic;
using System.IO;

namespace OsuDifficulty
{
    public class Beatmap
    {
        public readonly List<HitObject> HitObjects = new();
        public int ObjectCount { get; private set; }
        public int CircleCount { get; private set; }
        public double CircleSize { get; private set; }
        public double OverallDifficulty { get; private set; }
        public double ApproachRate { get; private set; }
        public double SliderTickRate { get; private set; }
        public string Title { get; private set; }
        public string Artist { get; private set; }
        public string Creator { get; private set; }
        public string Version { get; private set; }

        public void ParseBeatmapFile(string fileName)
        {
            string[] allLines = File.ReadAllLines(fileName);
            bool reachedHitObjectsLine = false;

            int objectCount = 0;
            int circleCount = 0;
            int sliderCount = 0;

            foreach (string line in allLines)
            {
                if (reachedHitObjectsLine)
                {
                    int x = int.Parse(line.Split(',')[0]);
                    int y = int.Parse(line.Split(',')[1]);
                    int time = int.Parse(line.Split(',')[2]);
                    int objectType = int.Parse(line.Split(',')[3]);
                    bool slider = false;
                    if (objectType != 12)
                    {
                        objectCount++;

                        if (!line.Contains("|"))
                        {
                            circleCount++;
                        }
                        else
                        {
                            sliderCount++;
                            slider = true;
                        }

                        HitObject hitObject = new(x, y, time, slider);
                        HitObjects.Add(hitObject);
                    }

                    continue;
                }

                if (line.Contains("Title:"))
                {
                    Title = line.Split(new[] {':'}, 2)[1];
                    continue;
                }

                if (line.Contains("Artist:"))
                {
                    Artist = line.Split(new[] {':'}, 2)[1];
                    continue;
                }

                if (line.Contains("Creator:"))
                {
                    Creator = line.Split(new[] {':'}, 2)[1];
                    continue;
                }

                if (line.Contains("Version:"))
                {
                    Version = line.Split(new[] {':'}, 2)[1];
                    continue;
                }

                if (line.Contains("CircleSize:"))
                {
                    CircleSize = Convert.ToDouble(line.Split(':')[1]);
                    continue;
                }

                if (line.Contains("OverallDifficulty:"))
                {
                    OverallDifficulty = Convert.ToDouble(line.Split(':')[1]);
                    ApproachRate = OverallDifficulty;
                    continue;
                }

                if (line.Contains("ApproachRate:"))
                {
                    ApproachRate = Convert.ToDouble(line.Split(':')[1]);
                    continue;
                }

                if (line.Contains("SliderTickRate:"))
                {
                    SliderTickRate = Convert.ToDouble(line.Split(':')[1]);
                    continue;
                }

                if (line.Contains("[HitObjects]")) reachedHitObjectsLine = true;
            }

            CircleCount = circleCount;
            ObjectCount = objectCount;
        }
    }
}