using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using HorizonFaser.Models;
using HorizonFaser.Network;

namespace HorizonFaser.UI
{
    [System.Serializable]
    public class PermitData
    {
        public string id;
        public string name;
        public string previewImageUrl;
        public string pdfUrl;
    }

    public static class JsonHelper
    {
        public static T[] FromJson<T>(string json)
        {
            string newJson = "{ \"array\": " + json + "}";
            Wrapper<T> wrapper = UnityEngine.JsonUtility.FromJson<Wrapper<T>>(newJson);
            return wrapper.array;
        }

        [System.Serializable]
        public class Wrapper<T>
        {
            public T[] array;
        }
    }

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
        public Button permitButton;
        public Button hideButton;

        public static bool IsAnyCardOpen = false;
        public static NodeInfoCardUI ActiveCard = null;

        private List<Button> _activeButtons = new List<Button>();
        private int _currentButtonIndex = 0;

        private Vector3 _baseScale = new Vector3(0.0013f, 0.0013f, 0.0013f);
        private float _currentAnimFactor = 0f;
        private Transform _cameraTransform;
        private string _baseDetails = "";
        
        private Component _grabInteractable;
        private System.Reflection.PropertyInfo _isSelectedProp;
        
        public Crew AssignedCrew;
        public Machine AssignedMachine;
        private bool _showingAdvancedInfo = false;
        private long _currentSegmentId = -1;
        
        private GameObject _currentTargetNode;
        public GameObject CurrentTargetNode => _currentTargetNode;
        public long CurrentSegmentId => _currentSegmentId;

        private static GameObject _detailsPopup;
        private static RawImage _popupImage;
        private static GameObject _permitPopup;
        private static GameObject _openPdfBtnObj;
        private static TextMeshProUGUI _openPdfBtnText;
        private static string _currentPdfUrl;
        private bool _isSmallSize = false;

        private void Awake()
        {
            if (Camera.main != null) _cameraTransform = Camera.main.transform;
            gameObject.SetActive(false);
            
            _grabInteractable = GetComponent("XRGrabInteractable");
            if (_grabInteractable != null)
            {
                _isSelectedProp = _grabInteractable.GetType().GetProperty("isSelected");
            }
        }

        public bool IsBeingGrabbed()
        {
            if (_grabInteractable == null)
            {
                _grabInteractable = GetComponent("XRGrabInteractable");
                if (_grabInteractable != null)
                {
                    _isSelectedProp = _grabInteractable.GetType().GetProperty("isSelected");
                }
            }

            if (_grabInteractable != null && _isSelectedProp != null)
            {
                try
                {
                    return (bool)_isSelectedProp.GetValue(_grabInteractable);
                }
                catch { }
            }
            return false;
        }

        private void Update()
        {
            if (_cameraTransform == null && Camera.main != null)
            {
                _cameraTransform = Camera.main.transform;
            }

            if (_cameraTransform != null)
            {
                bool isGrabbed = IsBeingGrabbed();
                
                if (!isGrabbed)
                {
                    Vector3 toCamera = transform.position - _cameraTransform.position;
                    if (toCamera.sqrMagnitude > 0.001f)
                    {
                        transform.rotation = Quaternion.LookRotation(toCamera);
                    }
                }

                Vector3 toCamera2 = transform.position - _cameraTransform.position;
                float distance = toCamera2.magnitude;
                
                // Escala controlada y elegante: acotada estrictamente entre 0.9x y 1.35x para evitar que sea gigantesca
                float distanceMultiplier = Mathf.Clamp(distance / 1.25f, 0.9f, 1.35f);
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
                _baseScale = new Vector3(0.0010f, 0.0010f, 0.0010f);
                if (_permitPopup != null) _permitPopup.transform.localScale = new Vector3(0.0012f, 0.0012f, 0.0012f);
                if (_detailsPopup != null) _detailsPopup.transform.localScale = new Vector3(0.0012f, 0.0012f, 0.0012f);
            }
            else
            {
                _baseScale = new Vector3(0.0013f, 0.0013f, 0.0013f);
                if (_permitPopup != null) _permitPopup.transform.localScale = new Vector3(0.0015f, 0.0015f, 0.0015f);
                if (_detailsPopup != null) _detailsPopup.transform.localScale = new Vector3(0.0015f, 0.0015f, 0.0015f);
            }
        }

        public void RecenterCard()
        {
            Camera mainCam = Camera.main ?? UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (mainCam != null)
            {
                _cameraTransform = mainCam.transform;
                Vector3 camPos = mainCam.transform.position;
                Vector3 camFwd = mainCam.transform.forward;
                Vector3 flatFwd = new Vector3(camFwd.x, 0f, camFwd.z).normalized;
                if (flatFwd.sqrMagnitude < 0.01f) flatFwd = camFwd.normalized;

                Vector3 targetPos = camPos + flatFwd * 1.25f + Vector3.up * (-0.08f);
                transform.position = targetPos;
                transform.rotation = Quaternion.LookRotation(transform.position - camPos);
            }
        }

        public void ShowCard(string nodeType, string nodeName, string details, Vector3 spawnPosition, Crew crew = null, Machine machine = null, long segmentId = -1, GameObject targetNode = null)
        {
            IsAnyCardOpen = true;
            gameObject.SetActive(true);
            _currentAnimFactor = 1f;
            
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

            if (hideButton != null)
            {
                hideButton.gameObject.SetActive(_currentTargetNode != null);
                hideButton.onClick.RemoveAllListeners();
                hideButton.onClick.AddListener(OnHideNodeClicked);
            }

            ActiveCard = this;
            _activeButtons.Clear();
            if (crewButton != null && crewButton.gameObject.activeInHierarchy) _activeButtons.Add(crewButton);
            if (machineryButton != null && machineryButton.gameObject.activeInHierarchy) _activeButtons.Add(machineryButton);
            if (permitButton != null && permitButton.gameObject.activeInHierarchy) _activeButtons.Add(permitButton);
            if (hideButton != null && hideButton.gameObject.activeInHierarchy) _activeButtons.Add(hideButton);
            
            _currentButtonIndex = 0;
            HighlightCurrentButton();

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

            // Posicionar la tarjeta SIEMPRE delante del usuario en un ángulo cómodo y ergonómico
            Camera mainCam = Camera.main ?? UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (mainCam != null)
            {
                _cameraTransform = mainCam.transform;
                Vector3 camPos = mainCam.transform.position;
                Vector3 camFwd = mainCam.transform.forward;
                Vector3 flatFwd = new Vector3(camFwd.x, 0f, camFwd.z).normalized;
                if (flatFwd.sqrMagnitude < 0.01f) flatFwd = camFwd.normalized;

                // 1.25 metros adelante del usuario, a la altura visual de trabajo (-0.08m)
                Vector3 targetPos = camPos + flatFwd * 1.25f + Vector3.up * (-0.08f);
                transform.position = targetPos;
                transform.rotation = Quaternion.LookRotation(transform.position - camPos);
            }
            else
            {
                transform.position = spawnPosition + Vector3.up * 1.5f;
            }

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
            
            OpenDetailsPopup("DETALLES DE CUADRILLA", info, "https://images.unsplash.com/photo-1541888081622-1f486a237f8e?ixlib=rb-4.0.3&w=400&q=80");
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

        public void OnPermitButtonClicked()
        {
            if (_permitPopup == null)
            {
                _permitPopup = NodeInfoCardFactory.CreatePermitPopup(
                    onOpenPdf: (text) => {
                        _openPdfBtnText.text = "ABRIENDO...";
                        Debug.Log($"[PDF DEBUG] VER PDF button clicked! URL is: {_currentPdfUrl}");
                        if (!string.IsNullOrEmpty(_currentPdfUrl)) Application.OpenURL(_currentPdfUrl);
                        else _openPdfBtnText.text = "URL VACÍA";
                    },
                    onClose: () => { _permitPopup.SetActive(false); },
                    out _openPdfBtnObj,
                    out _openPdfBtnText
                );
            }
            _permitPopup.SetActive(true);

            if (Camera.main != null)
            {
                Vector3 camPos = Camera.main.transform.position;
                Vector3 forward = Camera.main.transform.forward;
                Vector3 horizontalForward = new Vector3(forward.x, 0, forward.z);
                if (horizontalForward.sqrMagnitude > 0.01f) horizontalForward.Normalize();
                else horizontalForward = forward.normalized;
                
                _permitPopup.transform.position = camPos + horizontalForward * 1.5f;
                _permitPopup.transform.rotation = Quaternion.LookRotation(_permitPopup.transform.position - camPos);
            }
            
            StartCoroutine(PermitNetworkService.FetchAndDisplayPermit(_currentSegmentId, _permitPopup, (url) => {
                _currentPdfUrl = url;
                if (_openPdfBtnObj != null) _openPdfBtnObj.SetActive(true);
            }));
        }

        private void OpenDetailsPopup(string title, string info, string imageUrl)
        {
            if (_detailsPopup == null)
            {
                _detailsPopup = NodeInfoCardFactory.CreateDetailsPopup(out _popupImage, () => { _detailsPopup.SetActive(false); });
            }

            TextMeshProUGUI[] texts = _detailsPopup.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in texts)
            {
                if (t.name == "PopupTitleText") t.text = title;
                if (t.name == "PopupDetailsText") t.text = info;
            }

            if (_popupImage != null)
            {
                _popupImage.texture = null; 
                if (imageUrl.StartsWith("http")) StartCoroutine(ImageNetworkLoader.LoadImageFromUrl(imageUrl, _popupImage));
                else
                {
                    Texture2D tex = Resources.Load<Texture2D>(imageUrl);
                    if (tex != null) _popupImage.texture = tex;
                }
            }

            _detailsPopup.SetActive(true);

            if (Camera.main != null)
            {
                Vector3 camPos = Camera.main.transform.position;
                Vector3 forward = Camera.main.transform.forward;
                Vector3 horizontalForward = new Vector3(forward.x, 0, forward.z);
                if (horizontalForward.sqrMagnitude > 0.01f) horizontalForward.Normalize();
                else horizontalForward = forward.normalized;
                
                _detailsPopup.transform.position = camPos + horizontalForward * 2.0f;
                _detailsPopup.transform.rotation = Quaternion.LookRotation(_detailsPopup.transform.position - camPos);
            }
            else
            {
                _detailsPopup.transform.position = this.transform.position + Vector3.up * 1f;
            }
        }

        public void CycleOption(int dir)
        {
            if (_activeButtons.Count == 0) return;
            _currentButtonIndex = (_currentButtonIndex + dir) % _activeButtons.Count;
            if (_currentButtonIndex < 0) _currentButtonIndex += _activeButtons.Count;
            HighlightCurrentButton();
        }

        public void SelectCurrentOption()
        {
            if (_activeButtons.Count == 0) return;
            if (_currentButtonIndex >= 0 && _currentButtonIndex < _activeButtons.Count)
            {
                _activeButtons[_currentButtonIndex].onClick.Invoke();
            }
        }

        private void HighlightCurrentButton()
        {
            for (int i = 0; i < _activeButtons.Count; i++)
            {
                var btn = _activeButtons[i];
                var img = btn.GetComponent<Image>();
                if (img != null)
                {
                    if (i == _currentButtonIndex)
                    {
                        img.color = new Color(0.8f, 0.8f, 1f, 1f);
                        btn.transform.localScale = Vector3.one * 1.15f;
                    }
                    else
                    {
                        img.color = new Color(0.2f, 0.2f, 0.2f, 1f);
                        if (btn == hideButton) img.color = new Color(0.2f, 0.2f, 0.2f, 1f);
                        else if (btn == permitButton) img.color = new Color(0.9f, 0.1f, 0.3f, 1f);
                        else img.color = new Color(0.1f, 0.5f, 0.9f, 1f); 
                        btn.transform.localScale = Vector3.one;
                    }
                }
            }
        }

        public void HideCard()
        {
            if (ActiveCard == this) ActiveCard = null;
            IsAnyCardOpen = false;
            if (!gameObject.activeInHierarchy) return;
            
            if (_detailsPopup != null) _detailsPopup.SetActive(false);
            if (_permitPopup != null) _permitPopup.SetActive(false);
            
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
            return NodeInfoCardFactory.CreateGeneratedCard();
        }
    }
}
