using System;
using UnityEngine;

namespace AdonisLife.World.Environment
{
    /// <summary>
    /// Runtime weather hook point. Holds the current weather state, exposes an event for
    /// rendering/gameplay systems to react to, and can advance through deterministic daily
    /// weather via <see cref="WeatherModel"/>. Rendering effects subscribe to
    /// <see cref="OnWeatherChanged"/>; this component intentionally spawns no VFX itself.
    /// </summary>
    public class WeatherSystem : MonoBehaviour
    {
        [SerializeField] private WeatherState _currentWeather = WeatherState.Clear;
        [SerializeField] private int _weatherSeed = 1234;
        [SerializeField] private int _currentDay;

        /// <summary>Raised whenever the weather state changes. Payload: the new state.</summary>
        public event Action<WeatherState> OnWeatherChanged;

        public WeatherState CurrentWeather => _currentWeather;
        public int CurrentDay => _currentDay;

        /// <summary>Forces a specific weather state, notifying subscribers on change.</summary>
        public void SetWeather(WeatherState state)
        {
            if (state == _currentWeather)
            {
                return;
            }

            _currentWeather = state;
            OnWeatherChanged?.Invoke(state);
        }

        /// <summary>Advances to the next day and applies its deterministic weather.</summary>
        public void AdvanceDay()
        {
            _currentDay++;
            SetWeather(WeatherModel.GetWeatherForDay(_currentDay, _weatherSeed));
        }
    }
}
