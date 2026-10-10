using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using CesiumForUnity;

namespace HorizonFaser.Sensors
{
    [System.Serializable]
    public class ARCompanionData
    {
        public double lat;
        public double lng;
        public double alt;
        public double heading;
        public double accuracy;
        public string timestamp;
        public long projectId;
        
        public string notificationTitle;
        public string notificationBody;
        public string notificationEntityId;
        public string notificationId;
        
        public bool forceCalibration;
        public double manualRotation;
    }

    public class ARCompanionListener : MonoBehaviour
    {
        public int listenPort = 8000;
        public CesiumGeoreference georeference;
        public bool autoUpdateGeoreference = true;
        private bool hasAlignedOnce = false;

        private UdpClient udpClient;
        private Thread receiveThread;
        private bool isRunning;

        // Utilizamos variables thread-safe para pasar los datos al hilo principal
        public ARCompanionData latestData;
        private bool hasNewData = false;
        private object dataLock = new object();
        
        // Timer for telemetry logging
        private float lastLogTime = 0f;

        void Start()
        {
            autoUpdateGeoreference = true; 
            
            if (georeference == null)
            {
                georeference = FindFirstObjectByType<CesiumGeoreference>();
            }

            StartListening();
        }

        void StartListening()
        {
            try
            {
                udpClient = new UdpClient(listenPort);
                isRunning = true;
                receiveThread = new Thread(new ThreadStart(ReceiveData));
                receiveThread.IsBackground = true;
                receiveThread.Start();
                Debug.Log($"[ARCompanionListener] Empezando a escuchar en el puerto UDP {listenPort}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ARCompanionListener] Error al iniciar UDP: {e.Message}");
            }
        }

        private void ReceiveData()
        {
            IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);

            while (isRunning)
            {
                try
                {
                    byte[] data = udpClient.Receive(ref anyIP);
                    string json = Encoding.UTF8.GetString(data);
                    
                    ARCompanionData parsedData = JsonUtility.FromJson<ARCompanionData>(json);
                    
                    if (parsedData != null)
                    {
                        lock (dataLock)
                        {
                            latestData = parsedData;
                            hasNewData = true;
                        }
                    }
                }
                catch (SocketException)
                {
                    // Ignore expected exceptions when closing socket
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[ARCompanionListener] Error parseando datos UDP: {e.Message}");
                }
            }
        }

        void Update()
        {
            if (hasNewData)
            {
                ARCompanionData dataToProcess;
                lock (dataLock)
                {
                    dataToProcess = latestData;
                    hasNewData = false;
                }

                ProcessData(dataToProcess);
            }
        }

        private long _currentProjectId = -1;
        private string _lastProcessedNotificationId = "";

        private void ProcessData(ARCompanionData data)
        {
            // Debug.Log($"[ARCompanionListener] Datos GPS recibidos - Lat: {data.lat}, Lng: {data.lng}, Project: {data.projectId}");

            if (data.projectId > 0 && data.projectId != _currentProjectId)
            {
                var dashboard = FindFirstObjectByType<HorizonFaser.UI.DashboardUIController>();
                if (dashboard != null)
                {
                    _currentProjectId = data.projectId;
                    dashboard.LoadProjectFromId(data.projectId);
                }
                else
                {
                    // No actualizamos _currentProjectId para que vuelva a intentar en el siguiente latido
                    Debug.LogWarning("[ARCompanionListener] DashboardUIController no encontrado. Reintentando en el próximo latido.");
                }
            }

            if (!string.IsNullOrEmpty(data.notificationTitle) && !string.IsNullOrEmpty(data.notificationId) && data.notificationId != _lastProcessedNotificationId)
            {
                _lastProcessedNotificationId = data.notificationId;
                var notifListener = FindFirstObjectByType<HorizonFaser.Network.NotificationListener>();
                if (notifListener != null)
                {
                    HorizonFaser.Network.SegmentNotification notif = new HorizonFaser.Network.SegmentNotification
                    {
                        title = data.notificationTitle,
                        body = data.notificationBody,
                        entityId = data.notificationEntityId,
                        channel = "UDP_BRIDGE"
                    };
                    notifListener.UpdateVisuals(notif);
                }
            }

            // Log telemetry continuously every 2 seconds
            if (Time.time - lastLogTime > 2f && data != null && data.lat != 0)
            {
                lastLogTime = Time.time;
                float camY = 0f;
                if (Camera.main != null) camY = Camera.main.transform.eulerAngles.y;
                
                string msg = $"[TELEMETRIA] Lat:{data.lat:F5} Lng:{data.lng:F5} Brújula:{data.heading:F1} | HeadsetRotY:{camY:F1}";
                if (HorizonFaser.Network.NotificationListener.Instance != null) {
                    HorizonFaser.Network.NotificationListener.Instance.LogToXR(msg);
                }
            }

            if (data != null && data.forceCalibration)
            {
                hasAlignedOnce = false; // Allow recalibration
            }

            if (autoUpdateGeoreference && !hasAlignedOnce && georeference != null) 
            {
                // Align world origin with current GPS position
                // Align world origin with current GPS position
                georeference.latitude = data.lat;
                georeference.longitude = data.lng;
                
                // CesiumGlobeAnchor ignora la rotación del padre en Unity. 
                // Para rotar el mundo virtual alrededor del usuario, rotamos el XR Rig (el usuario).
                if (data.heading > 0 && Camera.main != null)
                {
                    float targetCameraY = (float)data.heading - (float)data.manualRotation;
                    float currentCameraY = Camera.main.transform.eulerAngles.y;
                    float differenceY = targetCameraY - currentCameraY;

                    // Encontrar el XR Rig (root del player)
                    Transform xrRig = Camera.main.transform.parent;
                    while (xrRig != null && xrRig.name != "XR Origin" && xrRig.name != "XR Rig") {
                        if (xrRig.parent == null) break;
                        xrRig = xrRig.parent;
                    }
                    if (xrRig == null) xrRig = Camera.main.transform.root;

                    // Rotar el Rig alrededor de la posición exacta de la cámara para no desplazar al usuario
                    xrRig.RotateAround(Camera.main.transform.position, Vector3.up, differenceY);

                    if (HorizonFaser.Network.NotificationListener.Instance != null) {
                        HorizonFaser.Network.NotificationListener.Instance.LogToXR($"[CALIBRADO] Rig rotado: {differenceY:F1}°. CamY->{targetCameraY:F1}°");
                    }
                }
                
                hasAlignedOnce = true; // Desactivar para evitar jitter de rotación en cada frame
                string msg = $"[ALINEADO] Lat:{data.lat:F6} Lng:{data.lng:F6} Head:{data.heading:F1}";
                Debug.Log(msg);
                if (HorizonFaser.Network.NotificationListener.Instance != null) {
                    HorizonFaser.Network.NotificationListener.Instance.LogToXR(msg);
                }
            }
        }

        public void ForceAlignWithLastData()
        {
            lock (dataLock)
            {
                if (latestData != null && georeference != null)
                {
                    georeference.latitude = latestData.lat;
                    georeference.longitude = latestData.lng;
                    if (latestData.heading > 0)
                    {
                        georeference.transform.rotation = Quaternion.Euler(0, -(float)latestData.heading + 225f, 0);
                    }
                    Debug.Log($"[ARCompanionListener] Georeferencia alineada manualmente a: {latestData.lat}, {latestData.lng}");
                }
                else
                {
                    Debug.LogWarning("[ARCompanionListener] No hay datos GPS o Georeference falta para alinear.");
                }
            }
        }

        void OnDestroy()
        {
            isRunning = false;
            if (udpClient != null)
            {
                udpClient.Close();
            }
            if (receiveThread != null && receiveThread.IsAlive)
            {
                receiveThread.Abort();
            }
        }
    }
}
