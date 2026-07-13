namespace AdonisLife.World.Traffic
{
    /// <summary>Signal color shown to one axis of an intersection.</summary>
    public enum SignalColor
    {
        Red,
        Yellow,
        Green
    }

    /// <summary>The signal state of a whole intersection: one color per approach axis.</summary>
    public readonly struct IntersectionPhase
    {
        public readonly SignalColor NorthSouth;
        public readonly SignalColor EastWest;

        public IntersectionPhase(SignalColor northSouth, SignalColor eastWest)
        {
            NorthSouth = northSouth;
            EastWest = eastWest;
        }
    }

    /// <summary>
    /// Pure fixed-cycle traffic light timing: north-south green, yellow, then east-west green,
    /// yellow, repeating. The two axes are never permissive (green or yellow) at the same time.
    /// </summary>
    public static class TrafficLightModel
    {
        public const float GreenDuration = 12f;
        public const float YellowDuration = 3f;
        public const float CycleDuration = 2f * (GreenDuration + YellowDuration);

        /// <summary>Phase of the standard cycle at an absolute time in seconds.</summary>
        public static IntersectionPhase GetPhase(float timeSeconds)
        {
            float t = timeSeconds % CycleDuration;
            if (t < 0f)
            {
                t += CycleDuration;
            }

            if (t < GreenDuration)
            {
                return new IntersectionPhase(SignalColor.Green, SignalColor.Red);
            }

            if (t < GreenDuration + YellowDuration)
            {
                return new IntersectionPhase(SignalColor.Yellow, SignalColor.Red);
            }

            if (t < GreenDuration + YellowDuration + GreenDuration)
            {
                return new IntersectionPhase(SignalColor.Red, SignalColor.Green);
            }

            return new IntersectionPhase(SignalColor.Red, SignalColor.Yellow);
        }
    }
}
