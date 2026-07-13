using UnityEngine;

namespace AdonisLife.World.Authored
{
    [CreateAssetMenu(fileName = "ZoneConfig", menuName = "AdonisLife/World/ZoneConfig")]
    public class ZoneConfigSO : ScriptableObject, IWorldEntity
    {
        [SerializeField] private string _id;
        public string Id => _id;

        [SerializeField] private ZoneType _zoneType;
        public ZoneType ZoneType => _zoneType;

        [SerializeField] private float _maxBuildingHeight = 50f;
        public float MaxBuildingHeight => _maxBuildingHeight;

        [SerializeField] private float _densityLimit = 1.0f;
        public float DensityLimit => _densityLimit;
    }
}