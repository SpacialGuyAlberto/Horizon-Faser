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
            if (string.IsNullOrEmpty(sseEndpointUrl) || sseEndpointUrl.Contains("192.168.") || sseEndpointUrl.Contains("fedora.fritz.box"))
            {
                sseEndpointUrl = NetworkConfig.SseStreamUrl;
            }
        }

        public Font GetUIFont()
        {
            if (fontUI != null) return fontUI;
            fontUI = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (fontUI == null) fontUI = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (fontUI == null)
            {
                var allFonts = Resources.FindObjectsOfTypeAll<Font>();
                if (allFonts != null && allFonts.Length > 0) fontUI = allFonts[0];
            }
            return fontUI;
        }

        private void Start()
        {
            // Busca los iconos automáticamente en la escena por su nombre (si no han sido asignados)
            if (excavationIcon == null) excavationIcon = GameObject.Find("ExcavationIcon");
            if (ductIcon == null) ductIcon = GameObject.Find("DuctIcon");
            if (completedIcon == null) completedIcon = GameObject.Find("CompletedIcon");

            fontUI = GetUIFont();

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
            if (mainCam == null) mainCam = UnityEngine.Object.FindAnyObjectByType<Camera>();
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
            debugText.font = GetUIFont();
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

#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.N))
            {
                UpdateVisuals(new SegmentNotification
                {
                    title = "Segmento Actualizado",
                    body = "Fase completada: DUCT_INSTALLATION en tramo 12",
                    channel = "TEST_EDITOR",
                    entityId = "1"
                });
            }
#endif
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
            if (notification == null) return;
            Debug.Log($"[HUD AR] Nueva notificación: {notification.title} - {notification.body}");
            LogToXR($"[NOTIFICACIÓN] {notification.title}: {notification.body}");
            
            // Ejemplo de cambio de icono global
            if (!string.IsNullOrEmpty(notification.body))
            {
                if (notification.body.Contains("EXCAVATION"))
                {
                    if (excavationIcon != null) excavationIcon.SetActive(false);
                    if (ductIcon != null) ductIcon.SetActive(true);
                }
                else if (notification.body.Contains("DUCT_INSTALLATION"))
                {
                    if (ductIcon != null) ductIcon.SetActive(false);
                    if (completedIcon != null) completedIcon.SetActive(true);
                }
            }

            // Anclar "toaster / marker" 3D en el segmento correspondiente
            if (!string.IsNullOrEmpty(notification.entityId))
            {
                HorizonFaser.XR.NetworkNodeInteractable[] interactables = UnityEngine.Object.FindObjectsByType<HorizonFaser.XR.NetworkNodeInteractable>();
                foreach (var interactable in interactables)
                {
                    if (interactable.segmentId.ToString() == notification.entityId)
                    {
                        SpawnBellNotification(interactable.gameObject, notification);
                        break; // Solo en este segmento
                    }
                }
            }

            // Mostrar Toaster Global en Pantalla (siempre enfrente en HUD superior XR)
            ShowGlobalToast(notification);
        }

        private void ShowGlobalToast(SegmentNotification notification)
        {
            Camera mainCam = Camera.main;
            if (mainCam == null) mainCam = UnityEngine.Object.FindAnyObjectByType<Camera>();

            // Canvas global persistente
            GameObject canvasObj = GameObject.Find("GlobalToastCanvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("GlobalToastCanvas");
                DontDestroyOnLoad(canvasObj);
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.overrideSorting = true;
                canvas.sortingOrder = 9999;
                
                RectTransform rt = canvasObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(620, 160);
                canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }

            Canvas c = canvasObj.GetComponent<Canvas>();
            if (c != null)
            {
                c.worldCamera = mainCam;
                c.overrideSorting = true;
                c.sortingOrder = 9999;
            }

            // Anclar al campo de visión del usuario (HUD superior cómodo y legible)
            // Escala 0.0016f para un canvas de 620px produce ~1.0m de ancho en VR a 1.2m de distancia.
            // Esto garantiza que el badge/toast sea perfectamente legible y destacado sin obstruir la vista central.
            if (mainCam != null)
            {
                canvasObj.transform.SetParent(mainCam.transform, false);
                canvasObj.transform.localPosition = new Vector3(0f, 0.22f, 1.20f); // 22cm arriba del centro, a 1.2m
                canvasObj.transform.localRotation = Quaternion.identity;
                canvasObj.transform.localScale = Vector3.one * 0.0016f;
            }
            else
            {
                canvasObj.transform.position = new Vector3(0f, 1.6f, 1.2f);
                canvasObj.transform.rotation = Quaternion.identity;
                canvasObj.transform.localScale = Vector3.one * 0.0016f;
            }

            // Limpiar toasts anteriores para no sobreponer
            foreach (Transform child in canvasObj.transform)
            {
                Destroy(child.gameObject);
            }

            // Contenedor del Toast animado
            GameObject toastObj = new GameObject($"Toast_{DateTime.Now.Ticks}");
            toastObj.transform.SetParent(canvasObj.transform, false);
            
            RectTransform toastRt = toastObj.AddComponent<RectTransform>();
            toastRt.anchorMin = new Vector2(0.5f, 0.5f); 
            toastRt.anchorMax = new Vector2(0.5f, 0.5f);
            toastRt.pivot = new Vector2(0.5f, 0.5f);
            toastRt.sizeDelta = new Vector2(600, 136);
            toastRt.anchoredPosition = new Vector2(0, 100); // Inicia desplazado arriba

            CanvasGroup group = toastObj.AddComponent<CanvasGroup>();
            group.alpha = 0f;

            // Borde luminoso violeta exterior (#6366F1 / #818CF8)
            UnityEngine.UI.Image outerGlow = toastObj.AddComponent<UnityEngine.UI.Image>();
            outerGlow.color = new Color(0.40f, 0.35f, 0.95f, 0.90f);
            outerGlow.sprite = HorizonFaser.UI.UISpriteFactory.GetRoundedSprite();
            outerGlow.type = UnityEngine.UI.Image.Type.Sliced;

            // Fondo interior oscuro (#0F1117 con 98% de opacidad), inset de 3px
            GameObject innerBgObj = new GameObject("InnerCard");
            innerBgObj.transform.SetParent(toastObj.transform, false);
            UnityEngine.UI.Image innerBg = innerBgObj.AddComponent<UnityEngine.UI.Image>();
            innerBg.color = new Color(0.065f, 0.075f, 0.10f, 0.98f);
            innerBg.sprite = HorizonFaser.UI.UISpriteFactory.GetRoundedSprite();
            innerBg.type = UnityEngine.UI.Image.Type.Sliced;
            RectTransform innerRt = innerBgObj.GetComponent<RectTransform>();
            innerRt.anchorMin = Vector2.zero; 
            innerRt.anchorMax = Vector2.one;
            innerRt.offsetMin = new Vector2(3, 3);
            innerRt.offsetMax = new Vector2(-3, -3);

            // Icono Pill (Campana con fondo suave)
            GameObject iconObj = new GameObject("IconPill");
            iconObj.transform.SetParent(innerBgObj.transform, false);
            UnityEngine.UI.Image iconPillBg = iconObj.AddComponent<UnityEngine.UI.Image>();
            iconPillBg.color = new Color(0.40f, 0.35f, 0.95f, 0.22f);
            iconPillBg.sprite = HorizonFaser.UI.UISpriteFactory.GetRoundedSprite();
            iconPillBg.type = UnityEngine.UI.Image.Type.Sliced;
            RectTransform iconPillRt = iconObj.GetComponent<RectTransform>();
            iconPillRt.anchorMin = new Vector2(0, 0.5f);
            iconPillRt.anchorMax = new Vector2(0, 0.5f);
            iconPillRt.pivot = new Vector2(0.5f, 0.5f);
            iconPillRt.anchoredPosition = new Vector2(38, 0);
            iconPillRt.sizeDelta = new Vector2(50, 50);

            GameObject iconTextObj = new GameObject("IconText");
            iconTextObj.transform.SetParent(iconObj.transform, false);
            UnityEngine.UI.Text iconText = iconTextObj.AddComponent<UnityEngine.UI.Text>();
            iconText.font = GetUIFont();
            iconText.text = "🔔";
            iconText.fontSize = 28;
            iconText.color = new Color(1f, 0.85f, 0.3f); // Ámbar brillante
            iconText.alignment = TextAnchor.MiddleCenter;
            RectTransform iconTextRt = iconTextObj.GetComponent<RectTransform>();
            iconTextRt.anchorMin = Vector2.zero;
            iconTextRt.anchorMax = Vector2.one;
            iconTextRt.sizeDelta = Vector2.zero;

            // Título
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(innerBgObj.transform, false);
            UnityEngine.UI.Text titleText = titleObj.AddComponent<UnityEngine.UI.Text>();
            titleText.font = GetUIFont();
            titleText.text = string.IsNullOrEmpty(notification.title) ? "ACTUALIZACIÓN" : notification.title.ToUpper();
            titleText.fontSize = 20;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleLeft;
            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 0.5f); 
            titleRt.anchorMax = new Vector2(1, 0.5f);
            titleRt.pivot = new Vector2(0, 0.5f);
            titleRt.anchoredPosition = new Vector2(76, 24);
            titleRt.sizeDelta = new Vector2(-230, 32);

            // Badge Pill (Ej: FIBRA / PROYECTO / CANAL)
            GameObject badgeObj = new GameObject("Badge");
            badgeObj.transform.SetParent(innerBgObj.transform, false);
            UnityEngine.UI.Image badgeBg = badgeObj.AddComponent<UnityEngine.UI.Image>();
            badgeBg.color = new Color(0.12f, 0.53f, 0.90f, 0.30f);
            badgeBg.sprite = HorizonFaser.UI.UISpriteFactory.GetPillSprite();
            badgeBg.type = UnityEngine.UI.Image.Type.Sliced;
            RectTransform badgeRt = badgeObj.GetComponent<RectTransform>();
            badgeRt.anchorMin = new Vector2(1, 0.5f); 
            badgeRt.anchorMax = new Vector2(1, 0.5f);
            badgeRt.pivot = new Vector2(1, 0.5f);
            badgeRt.anchoredPosition = new Vector2(-18, 24);
            badgeRt.sizeDelta = new Vector2(125, 28);

            GameObject badgeTextObj = new GameObject("BadgeText");
            badgeTextObj.transform.SetParent(badgeObj.transform, false);
            UnityEngine.UI.Text bText = badgeTextObj.AddComponent<UnityEngine.UI.Text>();
            bText.font = GetUIFont();
            bText.text = !string.IsNullOrEmpty(notification.channel) ? notification.channel.ToUpper() : "AVISO";
            bText.fontSize = 13;
            bText.fontStyle = FontStyle.Bold;
            bText.color = new Color(0.45f, 0.85f, 1f); // Azul cielo brillante
            bText.alignment = TextAnchor.MiddleCenter;
            RectTransform bTextRt = badgeTextObj.GetComponent<RectTransform>();
            bTextRt.anchorMin = Vector2.zero; 
            bTextRt.anchorMax = Vector2.one;
            bTextRt.sizeDelta = Vector2.zero;

            // Línea divisoria decorativa
            GameObject divObj = new GameObject("Divider");
            divObj.transform.SetParent(innerBgObj.transform, false);
            UnityEngine.UI.Image divImg = divObj.AddComponent<UnityEngine.UI.Image>();
            divImg.color = new Color(1f, 1f, 1f, 0.08f);
            RectTransform divRt = divObj.GetComponent<RectTransform>();
            divRt.anchorMin = new Vector2(0, 0.5f);
            divRt.anchorMax = new Vector2(1, 0.5f);
            divRt.pivot = new Vector2(0, 0.5f);
            divRt.anchoredPosition = new Vector2(76, 6);
            divRt.sizeDelta = new Vector2(-96, 1.5f);

            // Cuerpo del mensaje
            GameObject bodyObj = new GameObject("BodyText");
            bodyObj.transform.SetParent(innerBgObj.transform, false);
            UnityEngine.UI.Text bodyText = bodyObj.AddComponent<UnityEngine.UI.Text>();
            bodyText.font = GetUIFont();
            bodyText.text = notification.body;
            bodyText.fontSize = 16;
            bodyText.color = new Color(0.90f, 0.93f, 0.98f, 0.95f);
            bodyText.alignment = TextAnchor.UpperLeft;
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Truncate;
            RectTransform bodyRt = bodyObj.GetComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0, 0.5f); 
            bodyRt.anchorMax = new Vector2(1, 0.5f);
            bodyRt.pivot = new Vector2(0, 0.5f);
            bodyRt.anchoredPosition = new Vector2(76, -24);
            bodyRt.sizeDelta = new Vector2(-96, 48);

            // Animar la entrada
            StartCoroutine(AnimateToast(toastRt, group, true, 0.35f));

            // Destruir después de 6.5 segundos
            StartCoroutine(DestroyToastAfterDelay(toastObj, toastRt, group, 6.5f));
        }

        private IEnumerator AnimateToast(RectTransform rt, CanvasGroup group, bool entering, float duration)
        {
            float elapsed = 0f;
            Vector2 startPos = entering ? new Vector2(0, 100) : new Vector2(0, 0);
            Vector2 endPos = entering ? new Vector2(0, 0) : new Vector2(0, 100);
            float startAlpha = entering ? 0f : 1f;
            float endAlpha = entering ? 1f : 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = t * t * (3f - 2f * t); // SmoothStep
                if (rt != null) rt.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                if (group != null) group.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
                yield return null;
            }
            if (rt != null) rt.anchoredPosition = endPos;
            if (group != null) group.alpha = endAlpha;
        }

        private IEnumerator DestroyToastAfterDelay(GameObject toastObj, RectTransform rt, CanvasGroup group, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (toastObj != null)
            {
                yield return StartCoroutine(AnimateToast(rt, group, false, 0.35f));
                Destroy(toastObj);
            }
        }

        private void SpawnBellNotification(GameObject targetSegment, SegmentNotification notification)
        {
            Vector3 worldPos = targetSegment.transform.position + Vector3.up * 1.5f;
            var nodeInteractable = targetSegment.GetComponent<HorizonFaser.XR.NetworkNodeInteractable>();
            if (nodeInteractable != null && nodeInteractable.linePoints != null && nodeInteractable.linePoints.Length > 0)
            {
                Vector3 midPoint = nodeInteractable.linePoints[nodeInteractable.linePoints.Length / 2];
                worldPos = targetSegment.transform.TransformPoint(midPoint) + Vector3.up * 1.5f;
            }

            // Crear el Canvas 3D (World Space) flotando sobre el segmento
            GameObject canvasObj = new GameObject($"NotificationBell_{notification.entityId}");
            canvasObj.transform.position = worldPos;
            
            // Añadir Billboard (se auto-orienta hacia la cámara del usuario)
            canvasObj.AddComponent<BillboardUI>();
            
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 500;
            canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 160);
            canvas.transform.localScale = Vector3.one * 0.0025f; // Legible a distancia AR (~1.25m de ancho en mundo real)

            // Panel exterior con borde brillante
            UnityEngine.UI.Image outerGlow = canvasObj.AddComponent<UnityEngine.UI.Image>();
            outerGlow.color = new Color(0.40f, 0.35f, 0.95f, 0.85f);
            outerGlow.sprite = HorizonFaser.UI.UISpriteFactory.GetRoundedSprite();
            outerGlow.type = UnityEngine.UI.Image.Type.Sliced;

            // Panel de fondo oscuro
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform, false);
            UnityEngine.UI.Image bgImage = bgObj.AddComponent<UnityEngine.UI.Image>();
            bgImage.color = new Color(0.086f, 0.090f, 0.102f, 0.96f);
            bgImage.sprite = HorizonFaser.UI.UISpriteFactory.GetRoundedSprite();
            bgImage.type = UnityEngine.UI.Image.Type.Sliced;
            RectTransform bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; 
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = new Vector2(3, 3);
            bgRt.offsetMax = new Vector2(-3, -3);

            // Ícono de campana o alerta
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(bgObj.transform, false);
            UnityEngine.UI.Text iconText = iconObj.AddComponent<UnityEngine.UI.Text>();
            iconText.font = GetUIFont();
            iconText.text = "🔔";
            iconText.fontSize = 32;
            iconText.color = new Color(1f, 0.85f, 0.3f);
            iconText.alignment = TextAnchor.MiddleCenter;
            RectTransform iconRt = iconObj.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0, 0.5f); 
            iconRt.anchorMax = new Vector2(0, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = new Vector2(38, 0);
            iconRt.sizeDelta = new Vector2(50, 50);

            // Título
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(bgObj.transform, false);
            UnityEngine.UI.Text titleText = titleObj.AddComponent<UnityEngine.UI.Text>();
            titleText.font = GetUIFont();
            titleText.text = string.IsNullOrEmpty(notification.title) ? "AVISO" : notification.title.ToUpper();
            titleText.fontSize = 20;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleLeft;
            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 0.5f); 
            titleRt.anchorMax = new Vector2(1, 0.5f);
            titleRt.pivot = new Vector2(0, 0.5f);
            titleRt.anchoredPosition = new Vector2(76, 24);
            titleRt.sizeDelta = new Vector2(-88, 32);

            // Cuerpo del texto
            GameObject bodyObj = new GameObject("BodyText");
            bodyObj.transform.SetParent(bgObj.transform, false);
            UnityEngine.UI.Text bodyText = bodyObj.AddComponent<UnityEngine.UI.Text>();
            bodyText.font = GetUIFont();
            bodyText.text = notification.body;
            bodyText.fontSize = 16;
            bodyText.color = new Color(1f, 1f, 1f, 0.90f);
            bodyText.alignment = TextAnchor.UpperLeft;
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Truncate;
            RectTransform bodyRt = bodyObj.GetComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0, 0.5f); 
            bodyRt.anchorMax = new Vector2(1, 0.5f);
            bodyRt.pivot = new Vector2(0, 0.5f);
            bodyRt.anchoredPosition = new Vector2(76, -22);
            bodyRt.sizeDelta = new Vector2(-88, 52);

            // Destruir después de 12 segundos
            Destroy(canvasObj, 12f);
        }
    }
}
