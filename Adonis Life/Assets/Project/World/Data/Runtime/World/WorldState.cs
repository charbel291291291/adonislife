using System;
using System.Collections.Generic;

namespace AdonisLife.World.Runtime
{
    [Serializable]
    public class WorldState
    {
        public string worldConfigId;
        public double timeOfDay;
        public float globalEconomyIndex;
        public List<DistrictState> districtStates = new List<DistrictState>();
        public List<ChunkState> chunkStates = new List<ChunkState>();
    }
}