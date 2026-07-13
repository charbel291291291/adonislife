using System.Collections.Generic;
using UnityEngine;

namespace AdonisLife.World.Traffic
{
    /// <summary>
    /// Runtime driver for one intersection's traffic lights. Timing comes from the pure
    /// <see cref="TrafficLightModel"/>; this component only tints the lamp renderers found under
    /// it (named with the <c>Lamp_NS_</c> / <c>Lamp_EW_</c> prefixes) via property blocks.
    /// </summary>
    public class TrafficLightController : MonoBehaviour
    {
        public const string NorthSouthLampPrefix = "Lamp_NS_";
        public const string EastWestLampPrefix = "Lamp_EW_";

        [SerializeField] private float _timeOffsetSeconds;

        private static readonly int ColorProperty = Shader.PropertyToID("_BaseColor");

        private readonly List<Renderer> _northSouthLamps = new List<Renderer>();
        private readonly List<Renderer> _eastWestLamps = new List<Renderer>();
        private MaterialPropertyBlock _propertyBlock;

        public IntersectionPhase CurrentPhase => TrafficLightModel.GetPhase(Time.time + _timeOffsetSeconds);

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            foreach (Renderer lamp in GetComponentsInChildren<Renderer>())
            {
                if (lamp.name.StartsWith(NorthSouthLampPrefix))
                {
                    _northSouthLamps.Add(lamp);
                }
                else if (lamp.name.StartsWith(EastWestLampPrefix))
                {
                    _eastWestLamps.Add(lamp);
                }
            }
        }

        private void Update()
        {
            IntersectionPhase phase = CurrentPhase;
            Apply(_northSouthLamps, phase.NorthSouth);
            Apply(_eastWestLamps, phase.EastWest);
        }

        private void Apply(List<Renderer> lamps, SignalColor color)
        {
            Color tint = GetTint(color);
            foreach (Renderer lamp in lamps)
            {
                lamp.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(ColorProperty, tint);
                lamp.SetPropertyBlock(_propertyBlock);
            }
        }

        private static Color GetTint(SignalColor color)
        {
            switch (color)
            {
                case SignalColor.Green: return new Color(0.1f, 0.9f, 0.2f);
                case SignalColor.Yellow: return new Color(0.95f, 0.8f, 0.1f);
                default: return new Color(0.9f, 0.1f, 0.1f);
            }
        }
    }
}
