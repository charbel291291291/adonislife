using UnityEngine;

namespace AdonisLife.World.Authored
{
    [CreateAssetMenu(fileName = "WorldConfig", menuName = "AdonisLife/World/WorldConfig")]
    public class WorldConfigSO : ScriptableObject, IWorldEntity
    {
        [SerializeField] private string _id;
        public string Id => _id;

        [SerializeField] private Vector2 _worldSize;
        public Vector2 WorldSize => _worldSize;

        [SerializeField] private float _chunkSize = 128f;
        public float ChunkSize => _chunkSize;

        [SerializeField] private DistrictConfigSO[] _districts;
        public DistrictConfigSO[] Districts => _districts;
    }
}