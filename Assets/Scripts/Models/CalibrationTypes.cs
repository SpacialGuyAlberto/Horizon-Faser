using System;
using UnityEngine;

namespace HorizonFaser.Models
{
    [Serializable]
    public class GeoPointWGS84
    {
        public double Latitude = 0.0;
        public double Longitude = 0.0;
        public double Altitude = 0.0;

        public GeoPointWGS84() {}

        public GeoPointWGS84(double inLat, double inLng, double inAlt = 0.0)
        {
            Latitude = inLat;
            Longitude = inLng;
            Altitude = inAlt;
        }
    }

    [Serializable]
    public class AnchorCalibrationData
    {
        public string AnchorId;
        public string MarkerQRCode;
        public GeoPointWGS84 RefGeoPosition;
        public Vector3 LocalOffset = Vector3.zero;
        public Quaternion LocalRotation = Quaternion.identity;
        public DateTime CalibratedAt;
    }
}
