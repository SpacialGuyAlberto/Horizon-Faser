using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using HorizonFaser.Models;

namespace HorizonFaser.UI
{
    /// <summary>
    /// Controlador para la tarjeta de información (Information Card) Premium FAANG Style
    /// </summary>
    public class NodeInfoCardUI : MonoBehaviour
    {
        [Header("UI Elements")]
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI typeText;
        public TextMeshProUGUI detailsText;
        public Image accentLine;
        public Image badgeBg;
        public Button crewButton;
        public Button machineryButton;

        public static bool IsAnyCardOpen = false;

        private Vector3 _baseScale = new Vector3(0.0025f, 0.0025f, 0.0025f);
        private float _currentAnimFactor = 0f;
        private Transform _cameraTransform;
        private string _baseDetails = "";
        
        public Crew AssignedCrew;
        public Machine AssignedMachine;
        private bool _showingAdvancedInfo = false;

        private void Awake()
        {
            if (Camera.main != null) _cameraTransform = Camera.main.transform;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_cameraTransform != null)
            {
                // 1. Billboard: Siempre mirar a la cámara
                Vector3 toCamera = transform.position - _cameraTransform.position;
                transform.rotation = Quaternion.LookRotation(toCamera);

                // 2. Escala Dinámica por Distancia: Para que sea legible a 100km o a 1 metro.
                float distance = toCamera.magnitude;
                // Si está a 2 metros, el multiplicador es 1. A 20 metros, será 10.
                float distanceMultiplier = Mathf.Max(1f, distance / 2f);
                
                // Aplicamos la escala base * multiplicador de distancia * factor de animación
                transform.localScale = _baseScale * distanceMultiplier * _currentAnimFactor;

                // 3. Actualizar la distancia en tiempo real
                if (detailsText != null && !_showingAdvancedInfo && !string.IsNullOrEmpty(_baseDetails))
                {
                    detailsText.text = _baseDetails + $"\nDistancia: {distance:F1} m";
                }
            }
        }

        private static void SetLayerRecursively(GameObject obj, int newLayer)
        {
            if (null == obj) return;
            obj.layer = newLayer;
            foreach (Transform child in obj.transform)
            {
                if (null == child) continue;
                SetLayerRecursively(child.gameObject, newLayer);
            }
        }

        public void ShowCard(string nodeType, string nodeName, string details, Vector3 spawnPosition, Crew crew = null, Machine machine = null)
        {
            IsAnyCardOpen = true;
            gameObject.SetActive(true);

            if (titleText != null) titleText.text = nodeName;
            if (typeText != null) typeText.text = nodeType.ToUpper();
            
            _baseDetails = details;
            _showingAdvancedInfo = false;
            
            AssignedCrew = crew;
            AssignedMachine = machine;

            if (detailsText != null) detailsText.text = _baseDetails;
            
            if (crewButton != null)
            {
                crewButton.gameObject.SetActive(crew != null);
            }
            if (machineryButton != null)
            {
                machineryButton.gameObject.SetActive(machine != null);
            }

            // Reset size
            RectTransform canvasRT = GetComponent<RectTransform>();
            if (canvasRT != null) canvasRT.sizeDelta = new Vector2(450, 220);
            if (detailsText != null)
            {
                RectTransform detailsRT = detailsText.GetComponent<RectTransform>();
                if (detailsRT != null) detailsRT.sizeDelta = new Vector2(-40, 95);
            }

            // Colores dinámicos estilo FAANG
            Color popColor = new Color(1f, 0.35f, 0.1f); // Vibrant Orange
            Color nvtColor = new Color(0.9f, 0.8f, 0.1f); // Vibrant Yellow
            Color segColor = new Color(0.1f, 0.6f, 1f); // Sleek Blue

            Color currentAccent = segColor;
            if (nodeType.Contains("Pop") || nodeType.Contains("POP")) currentAccent = popColor;
            else if (nodeType.Contains("Netz") || nodeType.Contains("NVT")) currentAccent = nvtColor;

            if (accentLine != null) accentLine.color = currentAccent;
            if (badgeBg != null)
            {
                Color badgeCol = currentAccent;
                badgeCol.a = 0.15f; // Transparencia sutil
                badgeBg.color = badgeCol;
                typeText.color = currentAccent; 
            }

            // Posicionar ligeramente arriba del nodo
            transform.position = spawnPosition + Vector3.up * 1.5f;

            StopAllCoroutines();
            StartCoroutine(AnimatePopIn());
        }

        private static GameObject _detailsPopup;
        private static RawImage _popupImage;

        public void OnCrewButtonClicked()
        {
            if (AssignedCrew == null) return;
            string info = $"<size=150%><b><color=#00ff00>CUADRILLA ASIGNADA</color></b></size>\n";
            info += $"<color=#cccccc><b>Nombre:</b></color> {AssignedCrew.Name}\n";
            info += $"<color=#cccccc><b>Capataz:</b></color> {AssignedCrew.ForemanName}\n";
            info += $"<color=#cccccc><b>Tamaño:</b></color> {AssignedCrew.Size} personas\n";
            info += $"<color=#cccccc><b>Vehículo:</b></color> {AssignedCrew.VehiclePlate}\n\n";
            
            OpenDetailsPopup("DETALLES DE CUADRILLA", info, "https://images.unsplash.com/photo-1541888081622-1f486a237f8e?ixlib=rb-4.0.3&w=400&q=80"); // Construction workers image
        }

        public void OnMachineryButtonClicked()
        {
            if (AssignedMachine == null) return;
            string info = $"<size=150%><b><color=#ff8800>MAQUINARIA PESADA</color></b></size>\n";
            info += $"<color=#cccccc><b>Modelo:</b></color> {AssignedMachine.Name} ({AssignedMachine.Type})\n";
            info += $"<color=#cccccc><b>S/N:</b></color> {AssignedMachine.SerialNumber}\n";
            info += $"<color=#cccccc><b>Estatus:</b></color> {AssignedMachine.Status}\n";

            OpenDetailsPopup("DETALLES DE MAQUINARIA", info, "excavadora");
        }

        private void OpenDetailsPopup(string title, string info, string imageUrl)
        {
            if (_detailsPopup == null)
            {
                _detailsPopup = CreateDetailsPopup();
            }

            TextMeshProUGUI[] texts = _detailsPopup.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in texts)
            {
                if (t.name == "PopupTitleText") t.text = title;
                if (t.name == "PopupDetailsText") t.text = info;
            }

            if (_popupImage != null)
            {
                _popupImage.texture = null; // clear old image
                if (imageUrl.StartsWith("http"))
                {
                    StartCoroutine(LoadImageFromUrl(imageUrl, _popupImage));
                }
                else
                {
                    Texture2D tex = Resources.Load<Texture2D>(imageUrl);
                    if (tex != null) _popupImage.texture = tex;
                }
            }

            _detailsPopup.SetActive(true);

            // Position it 2 meters in front of the camera safely
            if (Camera.main != null)
            {
                Vector3 camPos = Camera.main.transform.position;
                Vector3 forward = Camera.main.transform.forward;
                
                // Si el usuario mira completamente hacia abajo/arriba, usar forward directo
                Vector3 horizontalForward = new Vector3(forward.x, 0, forward.z);
                if (horizontalForward.sqrMagnitude > 0.01f)
                {
                    horizontalForward.Normalize();
                }
                else
                {
                    horizontalForward = forward.normalized; // fallback
                }
                
                _detailsPopup.transform.position = camPos + horizontalForward * 2.0f;
                
                Vector3 toCamera = _detailsPopup.transform.position - camPos;
                _detailsPopup.transform.rotation = Quaternion.LookRotation(toCamera);
            }
            else
            {
                _detailsPopup.transform.position = this.transform.position + Vector3.up * 1f;
            }
        }

        private IEnumerator LoadImageFromUrl(string url, RawImage rawImage)
        {
            using (var request = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
            {
                yield return request.SendWebRequest();
                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    Texture2D texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(request);
                    rawImage.texture = texture;
                }
            }
        }

        private GameObject CreateDetailsPopup()
        {
            GameObject canvasObj = new GameObject("Details_Popup");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            try { canvasObj.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>(); } catch { }

            if (Camera.main != null) canvas.worldCamera = Camera.main;

            RectTransform canvasRT = canvasObj.GetComponent<RectTransform>();
            canvasRT.sizeDelta = new Vector2(800, 450); // Modal apaisado
            canvasObj.transform.localScale = new Vector3(0.003f, 0.003f, 0.003f);

            // Background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform, false);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.05f, 0.06f, 0.08f, 0.98f);
            RectTransform bgRT = bgObj.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = Vector2.zero;

            // Title
            GameObject titleObj = new GameObject("PopupTitleText");
            titleObj.transform.SetParent(bgObj.transform, false);
            TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
            title.text = "TITULO";
            title.fontSize = 32;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.color = Color.white;
            RectTransform titleRT = titleObj.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0, 1); titleRT.anchorMax = new Vector2(1, 1);
            titleRT.pivot = new Vector2(0.5f, 1);
            titleRT.anchoredPosition = new Vector2(0, -20);
            titleRT.sizeDelta = new Vector2(-40, 40);

            // Image (Left Side)
            GameObject imgObj = new GameObject("PopupImage");
            imgObj.transform.SetParent(bgObj.transform, false);
            _popupImage = imgObj.AddComponent<RawImage>();
            _popupImage.color = new Color(0.2f, 0.2f, 0.2f, 1f); // loading state color
            RectTransform imgRT = imgObj.GetComponent<RectTransform>();
            imgRT.anchorMin = new Vector2(0, 0); imgRT.anchorMax = new Vector2(0, 1);
            imgRT.pivot = new Vector2(0, 0.5f);
            imgRT.anchoredPosition = new Vector2(30, -10); // offset para centrar con el titulo
            imgRT.sizeDelta = new Vector2(300, -120); // 300 ancho, margen arriba y abajo

            // Text Info (Right Side)
            GameObject detailsObj = new GameObject("PopupDetailsText");
            detailsObj.transform.SetParent(bgObj.transform, false);
            TextMeshProUGUI details = detailsObj.AddComponent<TextMeshProUGUI>();
            details.text = "Cargando datos...";
            details.fontSize = 28;
            details.color = Color.white;
            details.alignment = TextAlignmentOptions.TopLeft;
            details.textWrappingMode = TextWrappingModes.Normal;
            details.overflowMode = TextOverflowModes.Overflow;
            RectTransform detailsRT = detailsObj.GetComponent<RectTransform>();
            detailsRT.anchorMin = new Vector2(0, 0); detailsRT.anchorMax = new Vector2(1, 1);
            detailsRT.pivot = new Vector2(0.5f, 0.5f);
            detailsRT.anchoredPosition = new Vector2(180, -30); 
            detailsRT.sizeDelta = new Vector2(-380, -160); // Ocupar el resto a la derecha

            // Close Button
            GameObject btnObj = new GameObject("CloseButton");
            btnObj.transform.SetParent(bgObj.transform, false);
            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0.8f, 0.2f, 0.2f, 1f);
            Button btn = btnObj.AddComponent<Button>();
            RectTransform btnRT = btnObj.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.5f, 0); btnRT.anchorMax = new Vector2(0.5f, 0);
            btnRT.pivot = new Vector2(0.5f, 0);
            btnRT.anchoredPosition = new Vector2(0, 30);
            btnRT.sizeDelta = new Vector2(200, 50);

            GameObject btnTextObj = new GameObject("BtnText");
            btnTextObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
            btnText.text = "CERRAR";
            btnText.fontSize = 20;
            btnText.fontStyle = FontStyles.Bold;
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.color = Color.white;
            RectTransform btnTextRT = btnTextObj.GetComponent<RectTransform>();
            btnTextRT.anchorMin = Vector2.zero; btnTextRT.anchorMax = Vector2.one;
            btnTextRT.sizeDelta = Vector2.zero;

            btn.onClick.AddListener(() => {
                canvasObj.SetActive(false);
            });

            SetLayerRecursively(canvasObj, 5); 
            canvasObj.SetActive(false);
            return canvasObj;
        }

        public void HideCard()
        {
            IsAnyCardOpen = false;
            if (!gameObject.activeInHierarchy) return;
            
            if (_detailsPopup != null)
            {
                _detailsPopup.SetActive(false);
            }
            
            StopAllCoroutines();
            StartCoroutine(AnimatePopOut());
        }

        private IEnumerator AnimatePopIn()
        {
            float t = 0;
            float duration = 0.4f;
            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                // Spring curve
                _currentAnimFactor = 1f - Mathf.Cos(t * Mathf.PI * 2f) * Mathf.Exp(-t * 8f);
                yield return null;
            }
            _currentAnimFactor = 1f;
        }

        private IEnumerator AnimatePopOut()
        {
            float t = 0;
            float duration = 0.15f;
            float startFactor = _currentAnimFactor;
            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                _currentAnimFactor = startFactor * (1f - (t * t)); // Ease out
                yield return null;
            }
            _currentAnimFactor = 0f;
            gameObject.SetActive(false);
        }

        public static NodeInfoCardUI CreateGeneratedCard()
        {
            // 1. Canvas World Space
            GameObject canvasObj = new GameObject("Premium_NodeInfoCard");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            // Agregar Raycaster para que los botones funcionen con el ratón o XR
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            // Para XR Interaction Toolkit
            try {
                canvasObj.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();
            } catch { }

            if (Camera.main != null)
            {
                canvas.worldCamera = Camera.main;
            }
            
            RectTransform canvasRT = canvasObj.GetComponent<RectTransform>();
            canvasRT.sizeDelta = new Vector2(450, 220); // Más ancho, proporciones cinemáticas

            // 2. Main Background (Glassmorphism oscuro)
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform, false);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.08f, 0.09f, 0.11f, 0.95f); // Gris muy oscuro, casi negro, elegante
            
            RectTransform bgRT = bgObj.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = Vector2.zero;

            // Drop Shadow falso para dar profundidad
            Shadow shadow = bgObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.5f);
            shadow.effectDistance = new Vector2(0, -5);

            // 3. Accent Line (Línea decorativa a la izquierda)
            GameObject accentObj = new GameObject("AccentLine");
            accentObj.transform.SetParent(bgObj.transform, false);
            Image accent = accentObj.AddComponent<Image>();
            accent.color = Color.white;
            
            RectTransform accentRT = accentObj.GetComponent<RectTransform>();
            accentRT.anchorMin = new Vector2(0, 0); accentRT.anchorMax = new Vector2(0, 1);
            accentRT.pivot = new Vector2(0, 0.5f);
            accentRT.sizeDelta = new Vector2(6, 0); // 6 pixeles de grosor

            // 4. Badge del Tipo (Pill shape background)
            GameObject badgeObj = new GameObject("BadgeBg");
            badgeObj.transform.SetParent(bgObj.transform, false);
            Image badge = badgeObj.AddComponent<Image>();
            badge.color = new Color(1, 1, 1, 0.15f);
            
            RectTransform badgeRT = badgeObj.GetComponent<RectTransform>();
            badgeRT.anchorMin = new Vector2(0, 1); badgeRT.anchorMax = new Vector2(0, 1);
            badgeRT.pivot = new Vector2(0, 1);
            badgeRT.anchoredPosition = new Vector2(25, -20);
            badgeRT.sizeDelta = new Vector2(150, 30);

            // Texto dentro del Badge
            GameObject typeObj = new GameObject("TypeText");
            typeObj.transform.SetParent(badgeObj.transform, false);
            TextMeshProUGUI type = typeObj.AddComponent<TextMeshProUGUI>();
            type.text = "NETWORK NODE";
            type.fontSize = 14;
            type.fontStyle = FontStyles.Bold;
            type.alignment = TextAlignmentOptions.Center;
            type.color = Color.white;
            
            RectTransform typeRT = typeObj.GetComponent<RectTransform>();
            typeRT.anchorMin = Vector2.zero; typeRT.anchorMax = Vector2.one;
            typeRT.sizeDelta = Vector2.zero;

            // 5. Texto del Título
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(bgObj.transform, false);
            TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
            title.text = "Identificador del Nodo";
            title.fontSize = 32;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Left;
            title.color = Color.white;
            
            RectTransform titleRT = titleObj.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0, 1); titleRT.anchorMax = new Vector2(1, 1);
            titleRT.pivot = new Vector2(0, 1);
            titleRT.anchoredPosition = new Vector2(25, -60);
            titleRT.sizeDelta = new Vector2(-40, 45);

            // 6. Texto de Detalles
            GameObject detailsObj = new GameObject("Details");
            detailsObj.transform.SetParent(bgObj.transform, false);
            TextMeshProUGUI details = detailsObj.AddComponent<TextMeshProUGUI>();
            details.text = "Latitud:\nLongitud:\nDistancia:";
            details.fontSize = 18;
            details.color = new Color(0.7f, 0.75f, 0.8f, 1f); // Gris claro azulado
            details.alignment = TextAlignmentOptions.TopLeft;
            
            RectTransform detailsRT = detailsObj.GetComponent<RectTransform>();
            detailsRT.anchorMin = new Vector2(0, 0); detailsRT.anchorMax = new Vector2(1, 0);
            detailsRT.pivot = new Vector2(0, 0);
            detailsRT.anchoredPosition = new Vector2(25, 20);
            detailsRT.sizeDelta = new Vector2(-40, 95);
            
            // 8. Crew Button
            GameObject crewBtnObj = new GameObject("CrewButton");
            crewBtnObj.transform.SetParent(bgObj.transform, false);
            Image crewBtnImg = crewBtnObj.AddComponent<Image>();
            crewBtnImg.color = new Color(0.1f, 0.6f, 0.2f, 1f); // Green
            Button cBtn = crewBtnObj.AddComponent<Button>();

            RectTransform cBtnRT = crewBtnObj.GetComponent<RectTransform>();
            cBtnRT.anchorMin = new Vector2(1, 0); cBtnRT.anchorMax = new Vector2(1, 0);
            cBtnRT.pivot = new Vector2(1, 0);
            cBtnRT.anchoredPosition = new Vector2(-150, 20); // a la izquierda del otro boton
            cBtnRT.sizeDelta = new Vector2(120, 40);

            GameObject cBtnTextObj = new GameObject("CBtnText");
            cBtnTextObj.transform.SetParent(crewBtnObj.transform, false);
            TextMeshProUGUI cBtnText = cBtnTextObj.AddComponent<TextMeshProUGUI>();
            cBtnText.text = "Crew Info";
            cBtnText.fontSize = 14;
            cBtnText.fontStyle = FontStyles.Bold;
            cBtnText.alignment = TextAlignmentOptions.Center;
            cBtnText.color = Color.white;
            RectTransform cBtnTextRT = cBtnTextObj.GetComponent<RectTransform>();
            cBtnTextRT.anchorMin = Vector2.zero; cBtnTextRT.anchorMax = Vector2.one;
            cBtnTextRT.sizeDelta = Vector2.zero;

            // 9. Machinery Button
            GameObject machBtnObj = new GameObject("MachineryButton");
            machBtnObj.transform.SetParent(bgObj.transform, false);
            Image machBtnImg = machBtnObj.AddComponent<Image>();
            machBtnImg.color = new Color(0.8f, 0.4f, 0.1f, 1f); // Orange
            Button mBtn = machBtnObj.AddComponent<Button>();

            RectTransform mBtnRT = machBtnObj.GetComponent<RectTransform>();
            mBtnRT.anchorMin = new Vector2(1, 0); mBtnRT.anchorMax = new Vector2(1, 0);
            mBtnRT.pivot = new Vector2(1, 0);
            mBtnRT.anchoredPosition = new Vector2(-20, 20); // a la derecha
            mBtnRT.sizeDelta = new Vector2(120, 40);

            GameObject mBtnTextObj = new GameObject("MBtnText");
            mBtnTextObj.transform.SetParent(machBtnObj.transform, false);
            TextMeshProUGUI mBtnText = mBtnTextObj.AddComponent<TextMeshProUGUI>();
            mBtnText.text = "Maquinaria";
            mBtnText.fontSize = 14;
            mBtnText.fontStyle = FontStyles.Bold;
            mBtnText.alignment = TextAlignmentOptions.Center;
            mBtnText.color = Color.white;
            RectTransform mBtnTextRT = mBtnTextObj.GetComponent<RectTransform>();
            mBtnTextRT.anchorMin = Vector2.zero; mBtnTextRT.anchorMax = Vector2.one;
            mBtnTextRT.sizeDelta = Vector2.zero;

            // 10. Close Button on main card
            GameObject closeBtnObj = new GameObject("CloseMainCardBtn");
            closeBtnObj.transform.SetParent(bgObj.transform, false);
            Image closeBtnImg = closeBtnObj.AddComponent<Image>();
            closeBtnImg.color = new Color(0.8f, 0.2f, 0.2f, 0.8f); // Red
            Button clBtn = closeBtnObj.AddComponent<Button>();

            RectTransform clBtnRT = closeBtnObj.GetComponent<RectTransform>();
            clBtnRT.anchorMin = new Vector2(1, 1); clBtnRT.anchorMax = new Vector2(1, 1);
            clBtnRT.pivot = new Vector2(1, 1);
            clBtnRT.anchoredPosition = new Vector2(-10, -10);
            clBtnRT.sizeDelta = new Vector2(30, 30);

            GameObject clBtnTextObj = new GameObject("ClBtnText");
            clBtnTextObj.transform.SetParent(closeBtnObj.transform, false);
            TextMeshProUGUI clBtnText = clBtnTextObj.AddComponent<TextMeshProUGUI>();
            clBtnText.text = "X";
            clBtnText.fontSize = 18;
            clBtnText.fontStyle = FontStyles.Bold;
            clBtnText.alignment = TextAlignmentOptions.Center;
            clBtnText.color = Color.white;
            RectTransform clBtnTextRT = clBtnTextObj.GetComponent<RectTransform>();
            clBtnTextRT.anchorMin = Vector2.zero; clBtnTextRT.anchorMax = Vector2.one;
            clBtnTextRT.sizeDelta = Vector2.zero;

            // 7. Ensamblar Script
            NodeInfoCardUI uiScript = canvasObj.AddComponent<NodeInfoCardUI>();
            uiScript.titleText = title;
            uiScript.typeText = type;
            uiScript.detailsText = details;
            uiScript.accentLine = accent;
            uiScript.badgeBg = badge;
            uiScript.crewButton = cBtn;
            uiScript.machineryButton = mBtn;
            
            cBtn.onClick.AddListener(uiScript.OnCrewButtonClicked);
            mBtn.onClick.AddListener(uiScript.OnMachineryButtonClicked);
            clBtn.onClick.AddListener(uiScript.HideCard);

            // IMPORTANTE PARA XR: Asignar al layer 'UI' (5) para que los rayos interactúen correctamente
            SetLayerRecursively(canvasObj, 5);

            canvasObj.SetActive(false);
            return uiScript;
        }
    }
}
