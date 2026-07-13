using UnityEngine;

namespace AdonisLife.World.Authored
{
    [CreateAssetMenu(fileName = "WeatherRegionConfig", menuName = "AdonisLife/World/Regions/WeatherRegionConfig")]
    public class WeatherRegionConfigSO : RegionConfigSO
    {
        [SerializeField] private float _temperatureBias = 0.0f;
        public float TemperatureBias => _temperatureBias;

        [SerializeField] private float _precipitationMultiplier = 1.0f;
        public float PrecipitationMultiplier => _precipitationMultiplier;
    }
}