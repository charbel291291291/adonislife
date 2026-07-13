using System;

namespace AdonisLife.World.Runtime
{
    [Serializable]
    public class ParcelState
    {
        public string parcelId;
        public string ownerPlayerId;
        public double marketValue; // Using double for serialization compatibility in standard Unity JsonUtility
        public bool isPowerGridConnected;
        public BuildingLotState buildingLotState;
    }
}