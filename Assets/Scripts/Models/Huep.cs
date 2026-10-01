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
        public double Elevation = 0.0; // Añadido para recibir la altitud del server
        public string InstallationLocation = "";
        public string Status = "";
        public long? NetzverteilerId;
    }
}
