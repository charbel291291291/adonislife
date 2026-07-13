using UnityEngine;

namespace AdonisLife.World.Authored
{
    [CreateAssetMenu(fileName = "AudioRegionConfig", menuName = "AdonisLife/World/Regions/AudioRegionConfig")]
    public class AudioRegionConfigSO : RegionConfigSO
    {
        [SerializeField] private string _ambientSnapshotName;
        public string AmbientSnapshotName => _ambientSnapshotName;

        [SerializeField] private float _reverbDecayTime = 1.0f;
        public float ReverbDecayTime => _reverbDecayTime;
    }
}