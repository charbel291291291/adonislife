using System;

namespace AdonisLife.World.Runtime
{
    [Serializable]
    public class DistrictState
    {
        public string districtId;
        public float currentTaxRate;
        public float dynamicPopularityIndex;
        public float currentSecurityLevel;
    }
}