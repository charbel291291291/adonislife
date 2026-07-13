using UnityEngine;

namespace AdonisLife.World.Authored
{
    [CreateAssetMenu(fileName = "ParcelConfig", menuName = "AdonisLife/World/ParcelConfig")]
    public class ParcelConfigSO : ScriptableObject, IWorldEntity
    {
        [SerializeField] private string _id;
        public string Id => _id;

        [SerializeField] private Vector2[] _vertices;
        public Vector2[] Vertices => _vertices;

        [SerializeField] private ZoneConfigSO _zoneConfig;
        public ZoneConfigSO ZoneConfig => _zoneConfig;

        [SerializeField] private BuildingLotConfigSO _buildingLot;
        public BuildingLotConfigSO BuildingLot => _buildingLot;
    }
}