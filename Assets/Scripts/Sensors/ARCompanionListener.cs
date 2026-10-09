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
    }

    public class ARCompanionListener : MonoBehaviour
    {
        public int listenPort = 8000;
        public CesiumGeoreference georeference;
        public bool autoUpdateGeoreference = false;

        private UdpClient udpClient;
        private Thread receiveThread;
        private bool isRunning;

        // Utilizamos variables thread-safe para pasar los datos al hilo principal
        public ARCompanionData latestData;
        private bool hasNewData = false;
        private object dataLock = new object();

        void Start()
        {
            autoUpdateGeoreference = false; // FORZADO: Evitar que Unity lo sobreescriba desde el Inspector
            
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

            if (autoUpdateGeoreference && georeference != null && data.accuracy < 10.0) // solo si es preciso
            {
                // Align world origin with current GPS position
                georeference.latitude = data.lat;
                georeference.longitude = data.lng;
                
                // Ignorar data.alt para forzar al usuario a aparecer al nivel del terreno de la DB
                // georeference.height = data.alt;
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
