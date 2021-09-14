namespace OsuDifficulty
{
    public class HitObject
    {
        public HitObject(int x, int y, int time, bool isSlider)
        {
            X = x;
            Y = y;
            Time = time;
            IsSlider = isSlider;
        }

        public int X { get; }
        public int Y { get; }
        public int Time { get; }
        public bool IsSlider { get; }
    }
}