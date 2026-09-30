using System;

namespace HorizonFaser.Models
{
    [Serializable]
    public class Huep
    {
        public long Id = 0;
        public string Identifier = "";
        public string StreetAddress = "";
        public string HouseNumber = "";
        public double Latitude = 0.0;
        public double Longitude = 0.0;
        public string InstallationLocation = "";
        public string Status = "";
        public long? NetzverteilerId;
    }
}
