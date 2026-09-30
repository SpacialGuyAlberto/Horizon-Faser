using System;

namespace HorizonFaser.Models
{
    public enum HazardType
    {
        GasPipeCrossing = 0, // Cruce de Tubería de Gas
        HighVoltagePower = 1, // Cable de Alta Tensión
        WaterMain = 2, // Canalización de Agua Potable
        Sewage = 3, // Red de Saneamiento
        RockObstacle = 4, // Roca Imprevista / Suelo Duro
        Other = 5 // Otro Obstáculo
    }

    [Serializable]
    public class IncidentReport
    {
        public long IncidentId = 0;
        public long SegmentId = 0;
        public HazardType HazardType = HazardType.GasPipeCrossing;
        public double Latitude = 0.0;
        public double Longitude = 0.0;
        public double DepthMeters = 0.0;
        public string Description;
        public string PhotoBase64OrUrl;
        public DateTime Timestamp;
    }
}
