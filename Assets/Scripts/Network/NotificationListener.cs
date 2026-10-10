using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System;

namespace HorizonFaser.Network
{
    [Serializable]
    public class SegmentNotification
    {
        public string recipientId;
        public string title;
        public string body;
        public string channel;
        public string entityId;
    }

    public class NotificationListener : MonoBehaviour
    {
        [Header("Backend Config")]
        public string sseEndpointUrl = "http://192.168.178.57:8080/api/notifications/stream";
        public string foremanId = "1"; // ID del capataz actual
        
        [Header("UI & Visuals")]
        public GameObject excavationIcon;
        public GameObject ductIcon;
        public GameObject completedIcon;
        
        public Font fontUI;

        // Esto creará automáticamente el NetworkManager y añadirá el script al iniciar el juego
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void AutoInitialize()
        {
            GameObject networkManager = new GameObject("NetworkManager");
            networkManager.AddComponent<NotificationListener>();
            DontDestroyOnLoad(networkManager);
            Debug.Log("[Network] NetworkManager auto-creado e inicializado.");
        }
        
        private UnityEngine.UI.Text debugText;
        public static NotificationListener Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void Start()
        {
            // Busca los iconos automáticamente en la escena por su nombre (si no han sido asignados)
            if (excavationIcon == null) excavationIcon = GameObject.Find("ExcavationIcon");
            if (ductIcon == null) ductIcon = GameObject.Find("DuctIcon");
            if (completedIcon == null) completedIcon = GameObject.Find("CompletedIcon");

            fontUI = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            CreateDebugLogUI();
            LogToXR("Iniciando NotificationListener...");
            
            try 
            {
                StartSSEClient();
            } 
            catch (Exception e) 
            {
                LogToXR("ERROR FATAL AL INICIAR: " + e.Message);
                LogToXR(e.StackTrace);
            }
        }

        private void CreateDebugLogUI()
        {
            GameObject debugCanvas = new GameObject("XRDebugCanvas");
            Canvas canvas = debugCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            Camera mainCam = Camera.main;
            if (mainCam == null) mainCam = UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (mainCam != null)
            {
                debugCanvas.transform.SetParent(mainCam.transform, false);
                debugCanvas.transform.localPosition = new Vector3(0, -0.4f, 1.2f); // Abajo en el centro
                debugCanvas.transform.localRotation = Quaternion.identity;
            }
            
            debugCanvas.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 400);
            debugCanvas.transform.localScale = Vector3.one * 0.001f;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(debugCanvas.transform, false);
            debugText = textObj.AddComponent<UnityEngine.UI.Text>();
            debugText.font = fontUI;
            debugText.fontSize = 14;
            debugText.color = Color.green;
            debugText.alignment = TextAnchor.LowerLeft;
            
            RectTransform rt = textObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
        }

        public void LogToXR(string message)
        {
            Debug.Log("[XR LOG] " + message);
            if (debugText != null)
            {
                debugText.text += "\n" + message;
                // Mantener solo las últimas 10 líneas
                string[] lines = debugText.text.Split('\n');
                if (lines.Length > 10)
                {
                    debugText.text = string.Join("\n", lines, lines.Length - 10, 10);
                }
            }
        }

        private System.Collections.Generic.Queue<Action> _mainThreadActions = new System.Collections.Generic.Queue<Action>();

        private void Update()
        {
            lock (_mainThreadActions)
            {
                while (_mainThreadActions.Count > 0)
                {
                    _mainThreadActions.Dequeue().Invoke();
                }
            }
        }

        private void EnqueueMainThread(Action action)
        {
            lock (_mainThreadActions)
            {
                _mainThreadActions.Enqueue(action);
            }
        }

        private void StartSSEClient()
        {
            System.Threading.Tasks.Task.Run(async () =>
            {
                string url = sseEndpointUrl + "?userId=" + foremanId;
                EnqueueMainThread(() => LogToXR("Conectando SSE nativo: " + url));
                
                try 
                {
                    using (var client = new System.Net.Http.HttpClient())
                    {
                        client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
                        client.DefaultRequestHeaders.Add("Accept", "text/event-stream");
                        
                        EnqueueMainThread(() => LogToXR("Enviando HTTP GET..."));
                        using (var response = await client.GetAsync(url, System.Net.Http.HttpCompletionOption.ResponseHeadersRead))
                        {
                            response.EnsureSuccessStatusCode();
                            EnqueueMainThread(() => LogToXR("¡Conectado 200 OK! Leyendo..."));
                            
                            using (var stream = await response.Content.ReadAsStreamAsync())
                            using (var reader = new System.IO.StreamReader(stream))
                            {
                                while (!reader.EndOfStream)
                                {
                                    string line = await reader.ReadLineAsync();
                                    if (!string.IsNullOrEmpty(line))
                                    {
                                        EnqueueMainThread(() => ProcessEvent(line));
                                    }
                                }
                            }
                        }
                    }
                } 
                catch (Exception e)
                {
                    EnqueueMainThread(() => LogToXR("Error red: " + e.Message));
                    await System.Threading.Tasks.Task.Delay(5000);
                    StartSSEClient(); // Reintentar
                }
            });
        }

        private void ProcessEvent(string rawEvent)
        {
            LogToXR("Procesando Evento...");
            string[] lines = rawEvent.Split('\n');
            foreach (var line in lines)
            {
                string tLine = line.Trim();
                if (tLine.StartsWith("data:"))
                {
                    try 
                    {
                        string json = tLine.Substring(5).Trim();
                        if (!json.StartsWith("{")) 
                        {
                            LogToXR("Ignorando payload no-JSON: " + json);
                            continue;
                        }

                        SegmentNotification notification = JsonUtility.FromJson<SegmentNotification>(json);
                        
                        if (notification != null && !string.IsNullOrEmpty(notification.title))
                        {
                            LogToXR("¡Notificación parseada! -> " + notification.title);
                            UpdateVisuals(notification);
                        }
                    }
                    catch (Exception e)
                    {
                        LogToXR("Excepción JSON: " + e.Message);
                        Debug.LogWarning("Error parseando notificación: " + e.Message);
                    }
                }
            }
        }

        public void UpdateVisuals(SegmentNotification notification)
        {
            Debug.Log($"[HUD AR] Nueva notificación: {notification.body}");
            
            // Ejemplo de cambio de icono global
            if (notification.body.Contains("EXCAVATION"))
            {
                if(excavationIcon != null) excavationIcon.SetActive(false);
                if(ductIcon != null) ductIcon.SetActive(true);
            }
            else if (notification.body.Contains("DUCT_INSTALLATION"))
            {
                if(ductIcon != null) ductIcon.SetActive(false);
                if(completedIcon != null) completedIcon.SetActive(true);
            }

            // Anclar "toaster" 3D en el segmento
            if (!string.IsNullOrEmpty(notification.entityId))
            {
                HorizonFaser.XR.NetworkNodeInteractable[] interactables = FindObjectsOfType<HorizonFaser.XR.NetworkNodeInteractable>();
                foreach (var interactable in interactables)
                {
                    if (interactable.segmentId.ToString() == notification.entityId)
                    {
                        SpawnBellNotification(interactable.gameObject, notification);
                        break; // Solo en uno de los cilindros de este segmento
                    }
                }
            }

            // Mostrar Toaster Global en Pantalla
            ShowGlobalToast(notification);
        }

        private void ShowGlobalToast(SegmentNotification notification)
        {
            Camera mainCam = Camera.main;
            if (mainCam == null) mainCam = UnityEngine.Object.FindFirstObjectByType<Camera>();

            // Canvas global persistente
            GameObject canvasObj = GameObject.Find("GlobalToastCanvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("GlobalToastCanvas");
                DontDestroyOnLoad(canvasObj);
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                
                RectTransform rt = canvasObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(400, 300);
                canvasObj.transform.localScale = Vector3.one * 0.00005f; // Reducido 20x
                
                canvasObj.AddComponent<BillboardUI>(); // Hace que siempre mire al usuario
                canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }

            // Reposicionar el Canvas justo en frente del usuario cada vez que sale una notificación
            if (mainCam != null)
            {
                canvasObj.transform.position = mainCam.transform.position + mainCam.transform.forward * 1.5f; // 1.5 metros adelante
                // La rotación será manejada por BillboardUI automáticamente
            }

            // Contenedor del Toast animado
            GameObject toastObj = new GameObject($"Toast_{DateTime.Now.Ticks}");
            toastObj.transform.SetParent(canvasObj.transform, false);
            
            RectTransform toastRt = toastObj.AddComponent<RectTransform>();
            toastRt.anchorMin = new Vector2(0.5f, 1f); 
            toastRt.anchorMax = new Vector2(0.5f, 1f);
            toastRt.pivot = new Vector2(0.5f, 1f);
            toastRt.sizeDelta = new Vector2(400, 100);
            toastRt.anchoredPosition = new Vector2(0, 150); // Empieza oculto arriba

            // Fondo Oscuro Industrial
            UnityEngine.UI.Image bgImage = toastObj.AddComponent<UnityEngine.UI.Image>();
            bgImage.color = new Color(0.086f, 0.090f, 0.102f, 0.98f); // #16171a
            bgImage.sprite = HorizonFaser.UI.UISpriteFactory.GetRoundedSprite();
            bgImage.type = UnityEngine.UI.Image.Type.Sliced;

            // Borde Cristalino
            GameObject borderObj = new GameObject("Border");
            borderObj.transform.SetParent(toastObj.transform, false);
            UnityEngine.UI.Image borderImage = borderObj.AddComponent<UnityEngine.UI.Image>();
            borderImage.color = new Color(1f, 1f, 1f, 0.15f);
            borderImage.sprite = HorizonFaser.UI.UISpriteFactory.GetRoundedSprite();
            borderImage.type = UnityEngine.UI.Image.Type.Sliced;
            RectTransform borderRt = borderObj.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero; borderRt.anchorMax = Vector2.one;
            borderRt.sizeDelta = Vector2.zero;

            // Título
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(toastObj.transform, false);
            UnityEngine.UI.Text titleText = titleObj.AddComponent<UnityEngine.UI.Text>();
            titleText.font = fontUI;
            titleText.text = notification.title.ToUpper();
            titleText.fontSize = 14;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleLeft;
            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 1); titleRt.anchorMax = new Vector2(1, 1);
            titleRt.pivot = new Vector2(0, 1);
            titleRt.anchoredPosition = new Vector2(20, -15);
            titleRt.sizeDelta = new Vector2(-120, 30);

            // Badge (Ej: UPDATE)
            GameObject badgeObj = new GameObject("Badge");
            badgeObj.transform.SetParent(toastObj.transform, false);
            UnityEngine.UI.Image badgeBg = badgeObj.AddComponent<UnityEngine.UI.Image>();
            badgeBg.color = new Color(0.23f, 0.51f, 0.96f, 0.2f); // Azul translúcido
            badgeBg.sprite = HorizonFaser.UI.UISpriteFactory.GetRoundedSprite();
            badgeBg.type = UnityEngine.UI.Image.Type.Sliced;
            RectTransform badgeRt = badgeObj.GetComponent<RectTransform>();
            badgeRt.anchorMin = new Vector2(1, 1); badgeRt.anchorMax = new Vector2(1, 1);
            badgeRt.pivot = new Vector2(1, 1);
            badgeRt.anchoredPosition = new Vector2(-20, -18);
            badgeRt.sizeDelta = new Vector2(100, 30); // Agrandado

            GameObject badgeTextObj = new GameObject("BadgeText");
            badgeTextObj.transform.SetParent(badgeObj.transform, false);
            UnityEngine.UI.Text bText = badgeTextObj.AddComponent<UnityEngine.UI.Text>();
            bText.font = fontUI;
            bText.text = "UPDATE";
            bText.fontSize = 16; // Texto más grande
            bText.fontStyle = FontStyle.Bold;
            bText.color = new Color(0.4f, 0.65f, 1f); // Azul claro
            bText.alignment = TextAnchor.MiddleCenter;
            RectTransform bTextRt = badgeTextObj.GetComponent<RectTransform>();
            bTextRt.anchorMin = Vector2.zero; bTextRt.anchorMax = Vector2.one;
            bTextRt.sizeDelta = Vector2.zero;

            // Cuerpo
            GameObject bodyObj = new GameObject("BodyText");
            bodyObj.transform.SetParent(toastObj.transform, false);
            UnityEngine.UI.Text bodyText = bodyObj.AddComponent<UnityEngine.UI.Text>();
            bodyText.font = fontUI;
            bodyText.text = notification.body;
            bodyText.fontSize = 14;
            bodyText.color = new Color(1f, 1f, 1f, 0.7f); // white/70
            bodyText.alignment = TextAnchor.UpperLeft;
            RectTransform bodyRt = bodyObj.GetComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0, 0); bodyRt.anchorMax = new Vector2(1, 1);
            bodyRt.pivot = new Vector2(0.5f, 0.5f);
            bodyRt.anchoredPosition = new Vector2(20, -25f);
            bodyRt.sizeDelta = new Vector2(-40, -50);

            // Animar la entrada
            StartCoroutine(AnimateToast(toastRt, true, 0.5f));

            // Destruir después de 5 segundos (y animar salida)
            StartCoroutine(DestroyToastAfterDelay(toastObj, toastRt, 5f));
        }

        private IEnumerator AnimateToast(RectTransform rt, bool entering, float duration)
        {
            float elapsed = 0f;
            Vector2 startPos = entering ? new Vector2(0, 150) : new Vector2(0, -20);
            Vector2 endPos = entering ? new Vector2(0, -20) : new Vector2(0, 150);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = t * t * (3f - 2f * t); // SmoothStep
                if (rt != null) rt.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                yield return null;
            }
            if (rt != null) rt.anchoredPosition = endPos;
        }

        private IEnumerator DestroyToastAfterDelay(GameObject toastObj, RectTransform rt, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (toastObj != null)
            {
                yield return StartCoroutine(AnimateToast(rt, false, 0.5f));
                Destroy(toastObj);
            }
        }

        private void SpawnBellNotification(GameObject targetSegment, SegmentNotification notification)
        {
            // Crear el Canvas 3D (World Space) flotando sobre el segmento
            GameObject canvasObj = new GameObject($"NotificationBell_{notification.entityId}");
            canvasObj.transform.position = targetSegment.transform.position + Vector3.up * 2.0f; // Flota a 2m
            canvasObj.transform.SetParent(targetSegment.transform);
            
            // Añadir Billboard (se auto-orienta hacia la cámara)
            var billboard = canvasObj.AddComponent<BillboardUI>();
            
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 150);
            canvas.transform.localScale = Vector3.one * 0.000025f; // Reducido x20 de 0.0005f

            // Panel de fondo oscuro
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform, false);
            UnityEngine.UI.Image bgImage = bgObj.AddComponent<UnityEngine.UI.Image>();
            bgImage.color = new Color(0.086f, 0.090f, 0.102f, 0.95f); // #16171a
            RectTransform bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            bgImage.sprite = HorizonFaser.UI.UISpriteFactory.GetRoundedSprite(); // Reutiliza el sprite redondeado
            bgImage.type = UnityEngine.UI.Image.Type.Sliced;

            // Borde cristalino
            GameObject borderObj = new GameObject("Border");
            borderObj.transform.SetParent(bgObj.transform, false);
            UnityEngine.UI.Image borderImage = borderObj.AddComponent<UnityEngine.UI.Image>();
            borderImage.color = new Color(1f, 1f, 1f, 0.1f);
            RectTransform borderRt = borderObj.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero; borderRt.anchorMax = Vector2.one;
            borderRt.sizeDelta = Vector2.zero;

            // Ícono de campana o alerta
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(bgObj.transform, false);
            UnityEngine.UI.Text iconText = iconObj.AddComponent<UnityEngine.UI.Text>();
            iconText.font = fontUI;
            iconText.text = "🔔";
            iconText.fontSize = 24;
            iconText.color = Color.yellow;
            iconText.alignment = TextAnchor.MiddleCenter;
            RectTransform iconRt = iconObj.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0, 1); iconRt.anchorMax = new Vector2(0, 1);
            iconRt.pivot = new Vector2(0, 1);
            iconRt.anchoredPosition = new Vector2(15, -15);
            iconRt.sizeDelta = new Vector2(30, 30);

            // Título
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(bgObj.transform, false);
            UnityEngine.UI.Text titleText = titleObj.AddComponent<UnityEngine.UI.Text>();
            titleText.font = fontUI;
            titleText.text = notification.title.ToUpper();
            titleText.fontSize = 14;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleLeft;
            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 1); titleRt.anchorMax = new Vector2(1, 1);
            titleRt.pivot = new Vector2(0, 1);
            titleRt.anchoredPosition = new Vector2(55, -15);
            titleRt.sizeDelta = new Vector2(-70, 30);

            // Cuerpo del texto
            GameObject bodyObj = new GameObject("BodyText");
            bodyObj.transform.SetParent(bgObj.transform, false);
            UnityEngine.UI.Text bodyText = bodyObj.AddComponent<UnityEngine.UI.Text>();
            bodyText.font = fontUI;
            bodyText.text = notification.body;
            bodyText.fontSize = 12;
            bodyText.color = new Color(1f, 1f, 1f, 0.7f); // white/70
            bodyText.alignment = TextAnchor.UpperLeft;
            RectTransform bodyRt = bodyObj.GetComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0, 0); bodyRt.anchorMax = new Vector2(1, 1);
            bodyRt.pivot = new Vector2(0.5f, 0.5f);
            bodyRt.anchoredPosition = new Vector2(27.5f, -25f);
            bodyRt.sizeDelta = new Vector2(-70, -60);

            // Destruir después de 10 segundos
            Destroy(canvasObj, 10f);
        }

    }
}
