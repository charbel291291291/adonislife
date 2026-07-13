using UnityEngine;

namespace AdonisLife.World.Authored
{
    public abstract class RegionConfigSO : ScriptableObject, IWorldEntity
    {
        [SerializeField] private string _id;
        public string Id => _id;

        [SerializeField] private RegionType _regionType;
        public RegionType RegionType => _regionType;

        [SerializeField] private Bounds _bounds;
        public Bounds Bounds => _bounds;
    }
}