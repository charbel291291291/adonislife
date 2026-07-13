using UnityEngine;

namespace AdonisLife.World.Authored
{
    [CreateAssetMenu(fileName = "ParkingRegionConfig", menuName = "AdonisLife/World/Regions/ParkingRegionConfig")]
    public class ParkingRegionConfigSO : RegionConfigSO
    {
        [SerializeField] private Matrix4x4[] _parkingSpots;
        public Matrix4x4[] ParkingSpots => _parkingSpots;

        [SerializeField] private float _baseHourlyRate = 2.50f;
        public float BaseHourlyRate => _baseHourlyRate;
    }
}