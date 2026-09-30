using System;

namespace HorizonFaser.Models
{
    [Serializable]
    public class Pop
    {
        public long Id = 0;
        public string Name = "";
        public string Identifier = "";
        public string LocationAddress = "";
        public double Latitude = 0.0;
        public double Longitude = 0.0;
        public int MaxCapacityPorts = 0;
        public long? ClusterId;
        public System.Collections.Generic.List<Netzverteiler> Nvts = new System.Collections.Generic.List<Netzverteiler>();
    }
}
