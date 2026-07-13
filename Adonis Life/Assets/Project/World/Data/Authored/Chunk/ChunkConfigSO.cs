using UnityEngine;

namespace AdonisLife.World.Authored
{
    [CreateAssetMenu(fileName = "ChunkConfig", menuName = "AdonisLife/World/ChunkConfig")]
    public class ChunkConfigSO : ScriptableObject, IWorldEntity
    {
        [SerializeField] private string _id;
        public string Id => _id;

        [SerializeField] private Vector2Int _chunkCoordinate;
        public Vector2Int ChunkCoordinate => _chunkCoordinate;

        [SerializeField] private ParcelConfigSO[] _parcels;
        public ParcelConfigSO[] Parcels => _parcels;

        [SerializeField] private RegionConfigSO[] _regions;
        public RegionConfigSO[] Regions => _regions;
    }
}