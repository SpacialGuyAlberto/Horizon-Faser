using System;
using System.Collections.Generic;

namespace HorizonFaser.Models
{
    public enum WorkflowState
    {
        Plan = 0, // Planificado (Rojo)
        TrenchOpen = 1, // Zanja Abierta (Amarillo)
        Blown = 2, // Fibra Soplada / Microtubo (Azul)
        Completed = 3 // Completado / Aufmaß (Verde)
    }

    public enum StreetType
    {
        GehwegPflaster = 0, // Acera Adoquinada
        GehwegAsphalt = 1, // Acera Asfalto
        StrasseAsphalt = 2, // Calzada Asfalto
        Unbefestigt = 3 // Tierra / Sin Pavimentar
    }

    public enum WorkType
    {
        CivilWorkTrenching = 0, // Obra Civil / Zanja
        MicroTrenching = 1, // Microzanja
        HorizontalDrilling = 2, // Perforación Dirigida
        SurfaceRestoration = 3 // Repavimentación
    }

    [Serializable]
    public class GeometryPoint
    {
        public double Latitude = 0.0;
        public double Longitude = 0.0;
        public double AltitudeZ = 0.0;

        public GeometryPoint() {}

        public GeometryPoint(double inLat, double inLng, double inAlt = 0.0)
        {
            Latitude = inLat;
            Longitude = inLng;
            AltitudeZ = inAlt;
        }
    }

    [Serializable]
    public class QuarkBauSegment
    {
        public long Id = 0;
        public long ProjectId = 0;
        public long? StartNvtId;
        public long? EndNvtId;
        public long? ConnectedPopId;
        public long? AssignedCrewId;
        public string StreetName;
        public string StreetType;
        public string WorkType;
        public string CurrentState;
        public string SoilType;
        public double LengthMeters = 0.0;
        public double TargetDepthMeters = 0.60;
        public double DuctDiameterMeters = 0.05;
        public double StartLatitude = 0.0;
        public double StartLongitude = 0.0;
        public double EndLatitude = 0.0;
        public double EndLongitude = 0.0;
        public List<GeometryPoint> Geometry = new List<GeometryPoint>();
        public string TrafficLevel;
        public string PlannedStartDate;
        public string PlannedEndDate;
        public string StartAddress;
        public string EndAddress;
        public string CustomFieldsJson;
        public bool IsDirtyLocal = false;
    }

    [Serializable]
    public class SyncQueueItem
    {
        public int QueueId = 0;
        public long SegmentId = 0;
        public string ActionType; // e.g. "UPDATE_STATE", "REPORT_INCIDENT", "UPDATE_GEOMETRY"
        public string PayloadJson;
        public DateTime CreatedAt;
        public int RetryCount = 0;
    }
}
