using UnityEngine;

namespace AdonisLife.World.Authored
{
    [CreateAssetMenu(fileName = "TrafficRegionConfig", menuName = "AdonisLife/World/Regions/TrafficRegionConfig")]
    public class TrafficRegionConfigSO : RegionConfigSO
    {
        [SerializeField] private Vector3[] _splineNodes;
        public Vector3[] SplineNodes => _splineNodes;

        [SerializeField] private float _speedLimit = 50f;
        public float SpeedLimit => _speedLimit;

        [SerializeField] private int _laneCount = 2;
        public int LaneCount => _laneCount;
    }
}