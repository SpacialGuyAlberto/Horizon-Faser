using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using HorizonFaser.Models;

namespace HorizonFaser.UI
{
    /// <summary>
    /// Controlador para la tarjeta de información (Information Card) Premium FAANG Style
    /// Refactorizado usando SOLID
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
        public Button permitButton;

        public static bool IsAnyCardOpen = false;

        private Vector3 _baseScale = new Vector3(0.0025f, 0.0025f, 0.0025f);
        private float _currentAnimFactor = 0f;
        private Transform _cameraTransform;
        private string _baseDetails = "";
        
        public Crew AssignedCrew;
        public Machine AssignedMachine;
        private bool _showingAdvancedInfo = false;
        private long _currentSegmentId = -1;
        
        private GameObject _currentTargetNode;
        private Button _hideButton;
        private bool _isSmallSize = false;

        private void Awake()
        {
            if (Camera.main != null) _cameraTransform = Camera.main.transform;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_cameraTransform != null)
            {
                Vector3 toCamera = transform.position - _cameraTransform.position;
                transform.rotation = Quaternion.LookRotation(toCamera);

                float distance = toCamera.magnitude;
                float distanceMultiplier = Mathf.Max(1f, distance / 2f);
                
                transform.localScale = _baseScale * distanceMultiplier * _currentAnimFactor;

                if (detailsText != null && !_showingAdvancedInfo && !string.IsNullOrEmpty(_baseDetails))
                {
                    detailsText.text = _baseDetails + $"\nDistancia: {distance:F1} m";
                }
            }
        }

        public void ToggleSize()
        {
            _isSmallSize = !_isSmallSize;
            if (_isSmallSize)
            {
                _baseScale = new Vector3(0.0012f, 0.0012f, 0.0012f);
                PermitPopupUI.SetScale(new Vector3(0.0015f, 0.0015f, 0.0015f));
                DetailsPopupUI.SetScale(new Vector3(0.0015f, 0.0015f, 0.0015f));
            }
            else
            {
                _baseScale = new Vector3(0.0025f, 0.0025f, 0.0025f);
                PermitPopupUI.SetScale(new Vector3(0.003f, 0.003f, 0.003f));
                DetailsPopupUI.SetScale(new Vector3(0.003f, 0.003f, 0.003f));
            }
        }

        public void ShowCard(string nodeType, string nodeName, string details, Vector3 spawnPosition, Crew crew = null, Machine machine = null, long segmentId = -1, GameObject targetNode = null)
        {
            IsAnyCardOpen = true;
            gameObject.SetActive(true);
            
            _currentSegmentId = segmentId;
            _currentTargetNode = targetNode;

            if (titleText != null) titleText.text = nodeName;
            if (typeText != null) typeText.text = nodeType.ToUpper();
            
            _baseDetails = details;
            _showingAdvancedInfo = false;
            
            AssignedCrew = crew;
            AssignedMachine = machine;

            if (detailsText != null) detailsText.text = _baseDetails;
            
            if (crewButton != null) crewButton.gameObject.SetActive(crew != null);
            if (machineryButton != null) machineryButton.gameObject.SetActive(machine != null);
            if (permitButton != null) permitButton.gameObject.SetActive(nodeType.Contains("Segmento") || nodeType.Contains("SEGMENTO"));

            if (_hideButton == null && permitButton != null)
            {
                _hideButton = Instantiate(permitButton, permitButton.transform.parent);
                _hideButton.gameObject.name = "HideButton";
                var txt = _hideButton.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = "OCULTAR";
                _hideButton.onClick.RemoveAllListeners();
                _hideButton.onClick.AddListener(OnHideNodeClicked);
                
                var bg = _hideButton.GetComponent<Image>();
                if (bg != null) bg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            }
            if (_hideButton != null)
            {
                _hideButton.gameObject.SetActive(_currentTargetNode != null);
            }

            RectTransform canvasRT = GetComponent<RectTransform>();
            if (canvasRT != null) canvasRT.sizeDelta = new Vector2(550, 220);
            if (detailsText != null)
            {
                RectTransform detailsRT = detailsText.GetComponent<RectTransform>();
                if (detailsRT != null) detailsRT.sizeDelta = new Vector2(-40, 95);
            }

            Color popColor = new Color(1f, 0.35f, 0.1f);
            Color nvtColor = new Color(0.9f, 0.8f, 0.1f);
            Color segColor = new Color(0.1f, 0.6f, 1f);

            Color currentAccent = segColor;
            if (nodeType.Contains("Pop") || nodeType.Contains("POP")) currentAccent = popColor;
            else if (nodeType.Contains("Netz") || nodeType.Contains("NVT")) currentAccent = nvtColor;

            if (accentLine != null) accentLine.color = currentAccent;
            if (badgeBg != null)
            {
                Color badgeCol = currentAccent;
                badgeCol.a = 0.15f;
                badgeBg.color = badgeCol;
                typeText.color = currentAccent; 
            }

            transform.position = spawnPosition + Vector3.up * 1.5f;

            StopAllCoroutines();
            StartCoroutine(AnimatePopIn());
        }

        public void OnHideNodeClicked()
        {
            if (_currentTargetNode != null)
            {
                _currentTargetNode.SetActive(false);
                HideCard();
                Debug.Log("[NodeInfoCardUI] Nodo ocultado por el usuario.");
            }
        }

        public void OnCrewButtonClicked()
        {
            if (AssignedCrew == null) return;
            string info = $"<size=150%><b><color=#00ff00>CUADRILLA ASIGNADA</color></b></size>\n";
            info += $"<color=#cccccc><b>Nombre:</b></color> {AssignedCrew.Name}\n";
            info += $"<color=#cccccc><b>Capataz:</b></color> {AssignedCrew.ForemanName}\n";
            info += $"<color=#cccccc><b>Tamaño:</b></color> {AssignedCrew.Size} personas\n";
            info += $"<color=#cccccc><b>Vehículo:</b></color> {AssignedCrew.VehiclePlate}\n\n";
            
            DetailsPopupUI.OpenDetailsPopup(this, "DETALLES DE CUADRILLA", info, "https://images.unsplash.com/photo-1541888081622-1f486a237f8e?ixlib=rb-4.0.3&w=400&q=80");
        }

        public void OnMachineryButtonClicked()
        {
            if (AssignedMachine == null) return;
            string info = $"<size=150%><b><color=#ff8800>MAQUINARIA PESADA</color></b></size>\n";
            info += $"<color=#cccccc><b>Modelo:</b></color> {AssignedMachine.Name} ({AssignedMachine.Type})\n";
            info += $"<color=#cccccc><b>S/N:</b></color> {AssignedMachine.SerialNumber}\n";
            info += $"<color=#cccccc><b>Estatus:</b></color> {AssignedMachine.Status}\n";

            DetailsPopupUI.OpenDetailsPopup(this, "DETALLES DE MAQUINARIA", info, "excavadora");
        }

        public void OnPermitButtonClicked()
        {
            PermitPopupUI.OpenPermitPopup(this, _currentSegmentId);
        }

        public void HideCard()
        {
            IsAnyCardOpen = false;
            if (!gameObject.activeInHierarchy) return;
            
            DetailsPopupUI.HideDetailsPopup();
            PermitPopupUI.HidePermitPopup();
            
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
                _currentAnimFactor = startFactor * (1f - (t * t));
                yield return null;
            }
            _currentAnimFactor = 0f;
            gameObject.SetActive(false);
        }

        public static NodeInfoCardUI CreateGeneratedCard()
        {
            GameObject canvasObj = new GameObject("Premium_NodeInfoCard");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            try {
                canvasObj.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();
            } catch { }

            if (Camera.main != null)
            {
                canvas.worldCamera = Camera.main;
            }
            
            RectTransform canvasRT = canvasObj.GetComponent<RectTransform>();
            canvasRT.sizeDelta = new Vector2(550, 220);

            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform, false);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.sprite = UISpriteFactory.GetRoundedSprite();
            bgImage.type = Image.Type.Sliced;
            bgImage.color = new Color(0.11f, 0.11f, 0.12f, 0.95f);
            
            RectTransform bgRT = bgObj.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = Vector2.zero;

            Shadow shadow = bgObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.5f);
            shadow.effectDistance = new Vector2(0, -5);

            GameObject accentObj = new GameObject("AccentLine");
            accentObj.transform.SetParent(bgObj.transform, false);
            Image accent = accentObj.AddComponent<Image>();
            accent.gameObject.SetActive(false); 

            GameObject badgeObj = new GameObject("BadgeBg");
            badgeObj.transform.SetParent(bgObj.transform, false);
            Image badge = badgeObj.AddComponent<Image>();
            badge.sprite = UISpriteFactory.GetPillSprite();
            badge.type = Image.Type.Sliced;
            badge.color = new Color(1, 1, 1, 0.15f);
            
            RectTransform badgeRT = badgeObj.GetComponent<RectTransform>();
            badgeRT.anchorMin = new Vector2(0, 1); badgeRT.anchorMax = new Vector2(0, 1);
            badgeRT.pivot = new Vector2(0, 1);
            badgeRT.anchoredPosition = new Vector2(25, -20);
            badgeRT.sizeDelta = new Vector2(150, 30);

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

            GameObject detailsObj = new GameObject("Details");
            detailsObj.transform.SetParent(bgObj.transform, false);
            TextMeshProUGUI details = detailsObj.AddComponent<TextMeshProUGUI>();
            details.text = "Latitud:\nLongitud:\nDistancia:";
            details.fontSize = 18;
            details.color = new Color(0.7f, 0.75f, 0.8f, 1f); 
            details.alignment = TextAlignmentOptions.TopLeft;
            
            RectTransform detailsRT = detailsObj.GetComponent<RectTransform>();
            detailsRT.anchorMin = new Vector2(0, 0); detailsRT.anchorMax = new Vector2(1, 0);
            detailsRT.pivot = new Vector2(0, 0);
            detailsRT.anchoredPosition = new Vector2(25, 20);
            detailsRT.sizeDelta = new Vector2(-40, 95);
            
            GameObject crewBtnObj = new GameObject("CrewButton");
            crewBtnObj.transform.SetParent(bgObj.transform, false);
            Image crewBtnImg = crewBtnObj.AddComponent<Image>();
            crewBtnImg.sprite = UISpriteFactory.GetPillSprite();
            crewBtnImg.type = Image.Type.Sliced;
            crewBtnImg.color = new Color(0.17f, 0.17f, 0.18f, 1f);
            Button cBtn = crewBtnObj.AddComponent<Button>();

            RectTransform cBtnRT = crewBtnObj.GetComponent<RectTransform>();
            cBtnRT.anchorMin = new Vector2(1, 0); cBtnRT.anchorMax = new Vector2(1, 0);
            cBtnRT.pivot = new Vector2(1, 0);
            cBtnRT.anchoredPosition = new Vector2(-280, 20);
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

            GameObject machBtnObj = new GameObject("MachineryButton");
            machBtnObj.transform.SetParent(bgObj.transform, false);
            Image machBtnImg = machBtnObj.AddComponent<Image>();
            machBtnImg.sprite = UISpriteFactory.GetPillSprite();
            machBtnImg.type = Image.Type.Sliced;
            machBtnImg.color = new Color(0.17f, 0.17f, 0.18f, 1f);
            Button mBtn = machBtnObj.AddComponent<Button>();

            RectTransform mBtnRT = machBtnObj.GetComponent<RectTransform>();
            mBtnRT.anchorMin = new Vector2(1, 0); mBtnRT.anchorMax = new Vector2(1, 0);
            mBtnRT.pivot = new Vector2(1, 0);
            mBtnRT.anchoredPosition = new Vector2(-150, 20);
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

            GameObject permitBtnObj = new GameObject("PermitButton");
            permitBtnObj.transform.SetParent(bgObj.transform, false);
            Image permitBtnImg = permitBtnObj.AddComponent<Image>();
            permitBtnImg.sprite = UISpriteFactory.GetPillSprite();
            permitBtnImg.type = Image.Type.Sliced;
            permitBtnImg.color = new Color(0f, 0.48f, 1f, 1f); 
            Button pBtn = permitBtnObj.AddComponent<Button>();

            RectTransform pBtnRT = permitBtnObj.GetComponent<RectTransform>();
            pBtnRT.anchorMin = new Vector2(1, 0); pBtnRT.anchorMax = new Vector2(1, 0);
            pBtnRT.pivot = new Vector2(1, 0);
            pBtnRT.anchoredPosition = new Vector2(-20, 20);
            pBtnRT.sizeDelta = new Vector2(120, 40);

            GameObject pBtnTextObj = new GameObject("PBtnText");
            pBtnTextObj.transform.SetParent(permitBtnObj.transform, false);
            TextMeshProUGUI pBtnText = pBtnTextObj.AddComponent<TextMeshProUGUI>();
            pBtnText.text = "View Permits";
            pBtnText.fontSize = 14;
            pBtnText.fontStyle = FontStyles.Bold;
            pBtnText.alignment = TextAlignmentOptions.Center;
            pBtnText.color = Color.white;
            RectTransform pBtnTextRT = pBtnTextObj.GetComponent<RectTransform>();
            pBtnTextRT.anchorMin = Vector2.zero; pBtnTextRT.anchorMax = Vector2.one;
            pBtnTextRT.sizeDelta = Vector2.zero;

            GameObject closeBtnObj = new GameObject("CloseMainCardBtn");
            closeBtnObj.transform.SetParent(bgObj.transform, false);
            Image closeBtnImg = closeBtnObj.AddComponent<Image>();
            closeBtnImg.sprite = UISpriteFactory.GetRoundedSprite();
            closeBtnImg.type = Image.Type.Sliced;
            closeBtnImg.color = new Color(0.17f, 0.17f, 0.18f, 1f);
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
            clBtnText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            RectTransform clBtnTextRT = clBtnTextObj.GetComponent<RectTransform>();
            clBtnTextRT.anchorMin = Vector2.zero; clBtnTextRT.anchorMax = Vector2.one;
            clBtnTextRT.sizeDelta = Vector2.zero;

            GameObject scaleBtnObj = new GameObject("ScaleMainCardBtn");
            scaleBtnObj.transform.SetParent(bgObj.transform, false);
            Image scaleBtnImg = scaleBtnObj.AddComponent<Image>();
            scaleBtnImg.sprite = UISpriteFactory.GetRoundedSprite();
            scaleBtnImg.type = Image.Type.Sliced;
            scaleBtnImg.color = new Color(0.17f, 0.17f, 0.18f, 1f);
            Button scBtn = scaleBtnObj.AddComponent<Button>();

            RectTransform scBtnRT = scaleBtnObj.GetComponent<RectTransform>();
            scBtnRT.anchorMin = new Vector2(1, 1); scBtnRT.anchorMax = new Vector2(1, 1);
            scBtnRT.pivot = new Vector2(1, 1);
            scBtnRT.anchoredPosition = new Vector2(-50, -10);
            scBtnRT.sizeDelta = new Vector2(30, 30);

            GameObject scBtnTextObj = new GameObject("ScBtnText");
            scBtnTextObj.transform.SetParent(scaleBtnObj.transform, false);
            TextMeshProUGUI scBtnText = scBtnTextObj.AddComponent<TextMeshProUGUI>();
            scBtnText.text = "S"; 
            scBtnText.fontSize = 16;
            scBtnText.fontStyle = FontStyles.Bold;
            scBtnText.alignment = TextAlignmentOptions.Center;
            scBtnText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            RectTransform scBtnTextRT = scBtnTextObj.GetComponent<RectTransform>();
            scBtnTextRT.anchorMin = Vector2.zero; scBtnTextRT.anchorMax = Vector2.one;
            scBtnTextRT.sizeDelta = Vector2.zero;

            NodeInfoCardUI uiScript = canvasObj.AddComponent<NodeInfoCardUI>();
            uiScript.titleText = title;
            uiScript.typeText = type;
            uiScript.detailsText = details;
            uiScript.accentLine = accent;
            uiScript.badgeBg = badge;
            uiScript.crewButton = cBtn;
            uiScript.machineryButton = mBtn;
            uiScript.permitButton = pBtn;
            
            cBtn.onClick.AddListener(uiScript.OnCrewButtonClicked);
            mBtn.onClick.AddListener(uiScript.OnMachineryButtonClicked);
            pBtn.onClick.AddListener(uiScript.OnPermitButtonClicked);
            clBtn.onClick.AddListener(uiScript.HideCard);
            scBtn.onClick.AddListener(uiScript.ToggleSize);

            XRInteractionHelper.SetLayerRecursively(canvasObj, 5);
            XRInteractionHelper.MakeGrabbable(canvasObj, canvasRT.sizeDelta);

            canvasObj.SetActive(false);
            return uiScript;
        }
    }
}
