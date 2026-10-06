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

        private void ProcessData(ARCompanionData data)
        {
            // Debug.Log($"[ARCompanionListener] Datos GPS recibidos - Lat: {data.lat}, Lng: {data.lng}, Project: {data.projectId}");

            if (data.projectId > 0 && data.projectId != _currentProjectId)
            {
                _currentProjectId = data.projectId;
                var dashboard = FindFirstObjectByType<HorizonFaser.UI.DashboardUIController>();
                if (dashboard != null)
                {
                    dashboard.LoadProjectFromId(data.projectId);
                }
                else
                {
                    Debug.LogWarning("[ARCompanionListener] DashboardUIController no encontrado. No se puede cargar el proyecto.");
                }
            }

            if (autoUpdateGeoreference && georeference != null && data.accuracy < 10.0) // solo si es preciso
            {
                // Align world origin with current GPS position
                georeference.latitude = data.lat;
                georeference.longitude = data.lng;
                
                // Nota: la altura (alt) a veces no es precisa, podríamos mantener height = 0
                georeference.height = data.alt;
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
