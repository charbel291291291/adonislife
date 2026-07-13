using System;

namespace AdonisLife.World.Runtime
{
    [Serializable]
    public class ZoneState
    {
        public string zoneConfigId;
        public float localDevelopmentScale;
        public float safetyRating;
    }
}