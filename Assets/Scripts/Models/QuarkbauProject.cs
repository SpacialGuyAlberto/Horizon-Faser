using System;
using System.Collections.Generic;

namespace HorizonFaser.Models
{
    [Serializable]
    public class QuarkbauProject
    {
        public long Id = 0;
        public string Name = "";
        public string Description = "";
        public DateTime StartDate;
        public DateTime EndDate;
        public List<QuarkBauSegment> Segments = new List<QuarkBauSegment>();
        public double Latitude = 0.0;
        public double Longitude = 0.0;
        public string Status = "ACTIVE";
        public double TotalLengthMeters = 0.0;
        public int CompletedSegments = 0;
    }
}
