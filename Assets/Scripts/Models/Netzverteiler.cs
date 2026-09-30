using System;

namespace HorizonFaser.Models
{
    [Serializable]
    public class Netzverteiler
    {
        public long Id = 0;
        public string Identifier = "";
        public string LocationAddress = "";
        public double Latitude = 0.0;
        public double Longitude = 0.0;
        public long? PopId;
    }
}
