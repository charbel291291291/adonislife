using AdonisLife.World.Common;

namespace AdonisLife.World.Environment
{
    /// <summary>Coarse phases of the day used by gameplay and lighting hooks.</summary>
    public enum DayPeriod
    {
        Night,
        Dawn,
        Day,
        Dusk
    }

    /// <summary>Weather states exposed to gameplay and rendering hooks.</summary>
    public enum WeatherState
    {
        Clear,
        Cloudy,
        Rain,
        Fog
    }

    /// <summary>
    /// Pure day/night math: hour normalization, sun pitch, and period classification.
    /// </summary>
    public static class DayNightModel
    {
        public const float HoursPerDay = 24f;
        public const float DawnStartHour = 5f;
        public const float DayStartHour = 8f;
        public const float DuskStartHour = 18f;
        public const float NightStartHour = 21f;

        /// <summary>Wraps any hour value into [0, 24).</summary>
        public static float NormalizeHour(float hour)
        {
            float normalized = hour % HoursPerDay;
            return normalized < 0f ? normalized + HoursPerDay : normalized;
        }

        /// <summary>
        /// Sun pitch in degrees for an hour of day: -90 at midnight, 0 at 06:00 (sunrise),
        /// +90 at noon, 180 at 18:00 (sunset).
        /// </summary>
        public static float GetSunPitch(float hour)
        {
            return NormalizeHour(hour) / HoursPerDay * 360f - 90f;
        }

        /// <summary>Coarse period of the day for an hour value.</summary>
        public static DayPeriod GetPeriod(float hour)
        {
            float h = NormalizeHour(hour);
            if (h >= DawnStartHour && h < DayStartHour)
            {
                return DayPeriod.Dawn;
            }

            if (h >= DayStartHour && h < DuskStartHour)
            {
                return DayPeriod.Day;
            }

            if (h >= DuskStartHour && h < NightStartHour)
            {
                return DayPeriod.Dusk;
            }

            return DayPeriod.Night;
        }
    }

    /// <summary>
    /// Pure deterministic weather selection: the weather for any day index is a stable function
    /// of the seed, weighted toward clear skies.
    /// </summary>
    public static class WeatherModel
    {
        public const float ClearChance = 0.5f;
        public const float CloudyChance = 0.25f;
        public const float RainChance = 0.15f;

        public static WeatherState GetWeatherForDay(int day, int seed)
        {
            float roll = DeterministicHash.Value01(day, 0, 5, seed);
            if (roll < ClearChance)
            {
                return WeatherState.Clear;
            }

            if (roll < ClearChance + CloudyChance)
            {
                return WeatherState.Cloudy;
            }

            if (roll < ClearChance + CloudyChance + RainChance)
            {
                return WeatherState.Rain;
            }

            return WeatherState.Fog;
        }
    }
}
