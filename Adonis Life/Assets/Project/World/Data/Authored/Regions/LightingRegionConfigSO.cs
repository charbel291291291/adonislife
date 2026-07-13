using UnityEngine;

namespace AdonisLife.World.Authored
{
    [CreateAssetMenu(fileName = "LightingRegionConfig", menuName = "AdonisLife/World/Regions/LightingRegionConfig")]
    public class LightingRegionConfigSO : RegionConfigSO
    {
        [SerializeField] private Color _ambientColorOverride = Color.white;
        public Color AmbientColorOverride => _ambientColorOverride;

        [SerializeField] private float _fogDensityOverride = 0.01f;
        public float FogDensityOverride => _fogDensityOverride;
    }
}