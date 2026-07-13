using System;

namespace AdonisLife.World.Runtime
{
    [Serializable]
    public class RegionState
    {
        public string regionId;
        public RegionType regionType;
        public bool isStateActive;

        // Flattened region properties for robust Unity serialization
        public int currentEntityCount;
        public long lastSpawnTimestamp;

        public bool isPathBlocked;
        public float dynamicSpeedMultiplier;

        public float speedDampeningMultiplier;
        public float congestionFactor;

        public string[] occupiedSpotVehicleIds;

        public float currentPrecipitation;
        public float localTemperature;
        public float windSpeedX;
        public float windSpeedY;

        public float intensityOverride;
        public float localizedTimeOffset;

        public float ambientVolumeMultiplier;
    }
}