using UnityEngine;

namespace AdonisLife.World.Authored
{
    [CreateAssetMenu(fileName = "DistrictConfig", menuName = "AdonisLife/World/DistrictConfig")]
    public class DistrictConfigSO : ScriptableObject, IWorldEntity
    {
        [SerializeField] private string _id;
        public string Id => _id;

        [SerializeField] private string _districtName;
        public string DistrictName => _districtName;

        [SerializeField] private Rect _boundaries;
        public Rect Boundaries => _boundaries;

        [SerializeField] private float _baseTaxRate = 0.05f;
        public float BaseTaxRate => _baseTaxRate;

        [SerializeField] private float _baseSecurityIndex = 1.0f;
        public float BaseSecurityIndex => _baseSecurityIndex;
    }
}