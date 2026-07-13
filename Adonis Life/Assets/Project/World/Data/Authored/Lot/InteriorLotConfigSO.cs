using UnityEngine;

namespace AdonisLife.World.Authored
{
    [CreateAssetMenu(fileName = "InteriorLotConfig", menuName = "AdonisLife/World/InteriorLotConfig")]
    public class InteriorLotConfigSO : ScriptableObject, IWorldEntity
    {
        [SerializeField] private string _id;
        public string Id => _id;

        [SerializeField] private Vector3 _localOffset;
        public Vector3 LocalOffset => _localOffset;

        [SerializeField] private Vector3 _size;
        public Vector3 Size => _size;

        [SerializeField] private string _category;
        public string Category => _category;
    }
}