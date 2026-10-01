using System;

namespace HorizonFaser.Models
{
    [Serializable]
    public class Muffe
    {
        public long Id = 0;
        public string Type = "";
        public double Latitude = 0.0;
        public double Longitude = 0.0;
        public double Elevation = 0.0; // Altitud
        public int SpliceCount = 0;
        public bool IsSealed = false;
        public long? SplicerSubcontractorId;
    }
}
