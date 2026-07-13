using UnityEngine;

namespace AdonisLife.World.Authored
{
    [CreateAssetMenu(fileName = "NavigationRegionConfig", menuName = "AdonisLife/World/Regions/NavigationRegionConfig")]
    public class NavigationRegionConfigSO : RegionConfigSO
    {
        [SerializeField] private int _areaType;
        public int AreaType => _areaType;

        [SerializeField] private float _movementCostMultiplier = 1.0f;
        public float MovementCostMultiplier => _movementCostMultiplier;
    }
}