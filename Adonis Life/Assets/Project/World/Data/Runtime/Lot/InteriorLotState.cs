using System;

namespace AdonisLife.World.Runtime
{
    [Serializable]
    public class InteriorLotState
    {
        public string interiorId;
        public string lesseePlayerId;
        public string tenantProfileId;
        public bool isDoorLocked;
    }
}