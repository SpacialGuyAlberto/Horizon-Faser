using System;
using System.Collections.Generic;

namespace HorizonFaser.Models
{
    [Serializable]
    public abstract class UtilityLine
    {
        public long id;
        public string utilityType;
        public List<GeometryPoint> geometry;
        public double depth;
        public string operatorName;
        public string contactPhone;
        public string hazardLevel;
        public string status;
    }

    [Serializable]
    public class WaterPipe : UtilityLine
    {
        public string pipeType;
        public string material;
        public int diameterMm;
        public string pressureLevel;

        public WaterPipe() { utilityType = "WATER"; }
    }

    [Serializable]
    public class GasPipe : UtilityLine
    {
        public string pressureClass;
        public string material;

        public GasPipe() { utilityType = "GAS"; }
    }

    [Serializable]
    public class ElectricityCable : UtilityLine
    {
        public string voltageCategory;
        public int voltageValue;
        public string shieldingType;
        public int numberOfCores;

        public ElectricityCable() { utilityType = "ELECTRICITY"; }
    }
}
