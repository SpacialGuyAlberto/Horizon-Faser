using System;
using System.Collections.Generic;

namespace HorizonFaser.Models
{
    [Serializable]
    public class FencingPlan
    {
        public long id;
        public long segmentId; 
        public string closureType;
        public List<FencingElement> elements;
    }

    [Serializable]
    public class FencingElement
    {
        public long id;
        /// <summary>
        /// Tipos sugeridos: "BARRIER" (Valla), "CONE" (Cono), "SIGN_STOP" (Letrero Stop), "TRAFFIC_LIGHT" (Semáforo)
        /// </summary>
        public string elementType; 
        public double latitude;
        public double longitude;
        public double? rotation; 
    }
}
