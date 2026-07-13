using UnityEngine;

namespace AdonisLife.World.Authored
{
    [CreateAssetMenu(fileName = "SpawnRegionConfig", menuName = "AdonisLife/World/Regions/SpawnRegionConfig")]
    public class SpawnRegionConfigSO : RegionConfigSO
    {
        [SerializeField] private string[] _spawnableEntityTypes;
        public string[] SpawnableEntityTypes => _spawnableEntityTypes;

        [SerializeField] private float[] _spawnWeights;
        public float[] SpawnWeights => _spawnWeights;

        [SerializeField] private int _maxSimultaneousSpawns = 20;
        public int MaxSimultaneousSpawns => _maxSimultaneousSpawns;
    }
}