using System;
using UnityEngine;

namespace AdonisLife.World.Environment
{
    /// <summary>
    /// Runtime day/night driver. Advances the time of day, rotates the assigned sun light, and
    /// raises hooks when the hour or the coarse day period changes. All math lives in
    /// <see cref="DayNightModel"/> so it stays unit-testable.
    /// </summary>
    public class DayNightCycle : MonoBehaviour
    {
        [SerializeField] private Light _sunLight;
        [SerializeField, Range(0f, 24f)] private float _timeOfDay = 12f;
        [SerializeField] private float _dayLengthSeconds = 600f;
        [SerializeField] private float _sunYawDegrees = -30f;

        private int _lastHour = -1;
        private DayPeriod _lastPeriod;

        /// <summary>Raised when the integer hour changes. Payload: the new hour (0-23).</summary>
        public event Action<int> OnHourChanged;

        /// <summary>Raised when the coarse day period changes. Payload: the new period.</summary>
        public event Action<DayPeriod> OnPeriodChanged;

        public float TimeOfDay => _timeOfDay;
        public DayPeriod CurrentPeriod => DayNightModel.GetPeriod(_timeOfDay);

        /// <summary>Sets the time of day directly (hour is wrapped into [0, 24)).</summary>
        public void SetTimeOfDay(float hour)
        {
            _timeOfDay = DayNightModel.NormalizeHour(hour);
            ApplyState();
        }

        private void Start()
        {
            _lastPeriod = CurrentPeriod;
            ApplyState();
        }

        private void Update()
        {
            if (_dayLengthSeconds > 0f)
            {
                _timeOfDay = DayNightModel.NormalizeHour(
                    _timeOfDay + Time.deltaTime * DayNightModel.HoursPerDay / _dayLengthSeconds);
            }

            ApplyState();
        }

        private void ApplyState()
        {
            if (_sunLight != null)
            {
                _sunLight.transform.rotation =
                    Quaternion.Euler(DayNightModel.GetSunPitch(_timeOfDay), _sunYawDegrees, 0f);
            }

            int hour = (int)_timeOfDay;
            if (hour != _lastHour)
            {
                _lastHour = hour;
                OnHourChanged?.Invoke(hour);
            }

            DayPeriod period = CurrentPeriod;
            if (period != _lastPeriod)
            {
                _lastPeriod = period;
                OnPeriodChanged?.Invoke(period);
            }
        }
    }
}
