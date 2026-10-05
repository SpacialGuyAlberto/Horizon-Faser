using System;

namespace HorizonFaser.Models
{
    public enum MachineStatus
    {
        AVAILABLE,
        IN_USE,
        MAINTENANCE,
        RETIRED
    }

    [Serializable]
    public class Machine
    {
        public long Id = 0;
        public string SerialNumber;
        public string Name;
        public string Type;
        public string Status; // or MachineStatus, but JSON often sends string
        public string CurrentLocation;
        public long? AssignedCrewId;
        public string CreatedAt;
        public string LastMaintenanceDate;
    }
}
