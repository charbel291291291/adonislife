using UnityEngine;

namespace AdonisLife.World.Authored
{
    [CreateAssetMenu(fileName = "BuildingLotConfig", menuName = "AdonisLife/World/BuildingLotConfig")]
    public class BuildingLotConfigSO : ScriptableObject, IWorldEntity
    {
        [SerializeField] private string _id;
        public string Id => _id;

        [SerializeField] private Vector3 _dimensions;
        public Vector3 Dimensions => _dimensions;

        [SerializeField] private int _maxOccupancy = 10;
        public int MaxOccupancy => _maxOccupancy;

        [SerializeField] private InteriorLotConfigSO[] _interiors;
        public InteriorLotConfigSO[] Interiors => _interiors;
    }
}