using System;
using System.Collections.Generic;

namespace AdonisLife.World.Runtime
{
    [Serializable]
    public class ChunkState
    {
        public string chunkId;
        public int coordinateX;
        public int coordinateY;
        public bool isLoaded;
        public List<string> trackedEntityIds = new List<string>();
        public List<ParcelState> parcelStates = new List<ParcelState>();
        public List<RegionState> regionStates = new List<RegionState>();
    }
}