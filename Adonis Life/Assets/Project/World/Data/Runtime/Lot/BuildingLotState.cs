using System;
using System.Collections.Generic;

namespace AdonisLife.World.Runtime
{
    [Serializable]
    public class BuildingLotState
    {
        public string buildingLotId;
        public float structuralHealth;
        public bool hasFireDamage;
        public List<string> currentOccupantIds = new List<string>();
        public List<InteriorLotState> interiorStates = new List<InteriorLotState>();
    }
}