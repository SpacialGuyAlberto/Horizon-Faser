using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using HorizonFaser.Models;

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

        public static bool IsAnyCardOpen = false;

        private static Sprite _roundedSprite;
        private static Sprite _pillSprite;

        public static Sprite GetRoundedSprite()
        {
            if (_roundedSprite != null) return _roundedSprite;
            int size = 64;
            int r = 16;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            Color transparent = new Color(1,1,1,0);
            for(int y=0; y<size; y++)
            {
                for(int x=0; x<size; x++)
                {
                    if(x < r && y < r && Vector2.Distance(new Vector2(x,y), new Vector2(r-1,r-1)) > r)
                        pixels[y*size+x] = transparent;
                    else if(x > size-r && y < r && Vector2.Distance(new Vector2(x,y), new Vector2(size-r,r-1)) > r)
                        pixels[y*size+x] = transparent;
                    else if(x < r && y > size-r && Vector2.Distance(new Vector2(x,y), new Vector2(r-1,size-r)) > r)
                        pixels[y*size+x] = transparent;
                    else if(x > size-r && y > size-r && Vector2.Distance(new Vector2(x,y), new Vector2(size-r,size-r)) > r)
                        pixels[y*size+x] = transparent;
                    else
                        pixels[y*size+x] = Color.white;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            _roundedSprite = Sprite.Create(tex, new Rect(0,0,size,size), new Vector2(0.5f,0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(r,r,r,r));
            return _roundedSprite;
        }

        public static Sprite GetPillSprite()
        {
            if (_pillSprite != null) return _pillSprite;
            int size = 64;
            int r = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            Color transparent = new Color(1,1,1,0);
            for(int y=0; y<size; y++)
            {
                for(int x=0; x<size; x++)
                {
                    if(x < r && y < r && Vector2.Distance(new Vector2(x,y), new Vector2(r-1,r-1)) > r)
                        pixels[y*size+x] = transparent;
                    else if(x > size-r && y < r && Vector2.Distance(new Vector2(x,y), new Vector2(size-r,r-1)) > r)
                        pixels[y*size+x] = transparent;
                    else if(x < r && y > size-r && Vector2.Distance(new Vector2(x,y), new Vector2(r-1,size-r)) > r)
                        pixels[y*size+x] = transparent;
                    else if(x > size-r && y > size-r && Vector2.Distance(new Vector2(x,y), new Vector2(size-r,size-r)) > r)
                        pixels[y*size+x] = transparent;
                    else
                        pixels[y*size+x] = Color.white;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            _pillSprite = Sprite.Create(tex, new Rect(0,0,size,size), new Vector2(0.5f,0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(r,r,r,r));
            return _pillSprite;
        }

        private Vector3 _baseScale = new Vector3(0.0025f, 0.0025f, 0.0025f);
        private float _currentAnimFactor = 0f;
        private Transform _cameraTransform;
        private string _baseDetails = "";
        
        public Crew AssignedCrew;
        public Machine AssignedMachine;
        private bool _showingAdvancedInfo = false;
        private long _currentSegmentId = -1;

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

        private static void MakeGrabbable(GameObject canvasObj, Vector2 size)
        {
            BoxCollider col = canvasObj.AddComponent<BoxCollider>();
            col.size = new Vector3(size.x, size.y, 50f); // 50f thickness to make it easily grabbable in VR
            
            Rigidbody rb = canvasObj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            
            try 
            {
                var grab = canvasObj.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
                grab.throwOnDetach = false;
            } 
            catch 
            {
                try 
                {
                    // Fallback to older XR Interaction Toolkit namespace
                    var type = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.XRGrabInteractable, Unity.XR.Interaction.Toolkit");
                    if (type != null)
                    {
                        var grabComponent = canvasObj.AddComponent(type);
                        type.GetProperty("throwOnDetach")?.SetValue(grabComponent, false);
                    }
                } catch { }
            }
        }

        private bool _isSmallSize = false;
        public void ToggleSize()
        {
            _isSmallSize = !_isSmallSize;
            if (_isSmallSize)
            {
                _baseScale = new Vector3(0.0012f, 0.0012f, 0.0012f);
                if (_permitPopup != null) _permitPopup.transform.localScale = new Vector3(0.0015f, 0.0015f, 0.0015f);
                if (_detailsPopup != null) _detailsPopup.transform.localScale = new Vector3(0.0015f, 0.0015f, 0.0015f);
            }
            else
            {
                _baseScale = new Vector3(0.0025f, 0.0025f, 0.0025f);
                if (_permitPopup != null) _permitPopup.transform.localScale = new Vector3(0.003f, 0.003f, 0.003f);
                if (_detailsPopup != null) _detailsPopup.transform.localScale = new Vector3(0.003f, 0.003f, 0.003f);
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

        public void ShowCard(string nodeType, string nodeName, string details, Vector3 spawnPosition, Crew crew = null, Machine machine = null, long segmentId = -1)
        {
            IsAnyCardOpen = true;
            gameObject.SetActive(true);
            
            _currentSegmentId = segmentId;

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
            if (permitButton != null)
            {
                permitButton.gameObject.SetActive(nodeType.Contains("Segmento") || nodeType.Contains("SEGMENTO"));
            }

            // Reset size
            RectTransform canvasRT = GetComponent<RectTransform>();
            if (canvasRT != null) canvasRT.sizeDelta = new Vector2(550, 220);
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

        private static GameObject _permitPopup;
        private static string _currentPdfUrl;
        private static GameObject _openPdfBtnObj;

        public void OnPermitButtonClicked()
        {
            if (_permitPopup == null)
            {
                _permitPopup = CreatePermitPopup();
            }
            _permitPopup.SetActive(true);

            if (Camera.main != null)
            {
                Vector3 camPos = Camera.main.transform.position;
                Vector3 forward = Camera.main.transform.forward;
                
                Vector3 horizontalForward = new Vector3(forward.x, 0, forward.z);
                if (horizontalForward.sqrMagnitude > 0.01f)
                    horizontalForward.Normalize();
                else
                    horizontalForward = forward.normalized;
                
                _permitPopup.transform.position = camPos + horizontalForward * 1.5f;
                Vector3 toCamera = _permitPopup.transform.position - camPos;
                _permitPopup.transform.rotation = Quaternion.LookRotation(toCamera);
            }
            
            StartCoroutine(FetchAndDisplayPermit(_currentSegmentId));
        }

        private IEnumerator FetchAndDisplayPermit(long segmentId)
        {
            Transform bgTrans = _permitPopup.transform.Find("PermitImage");
            Transform loadingTrans = _permitPopup.transform.Find("LoadingText");
            if (loadingTrans != null) 
            {
                loadingTrans.gameObject.SetActive(true);
                loadingTrans.GetComponent<TextMeshProUGUI>().text = "Cargando documento...\nPor favor espera";
            }

            if (bgTrans != null)
            {
                RawImage bgImage = bgTrans.GetComponent<RawImage>();
                bgImage.texture = null;
                bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f); // Loading color

#if UNITY_EDITOR
                string baseUrl = "http://localhost:8080";
#else
                string baseUrl = "http://192.168.178.57:8080";
#endif
                string url = $"{baseUrl}/api/v1/segments/{segmentId}/permits";
                
                Debug.Log("Fetching permits with HttpClient from: " + url);
                
                var fetchTask = System.Threading.Tasks.Task.Run(async () => {
                    using (System.Net.Http.HttpClient client = new System.Net.Http.HttpClient())
                    {
                        client.Timeout = System.TimeSpan.FromSeconds(60);
                        var response = await client.GetAsync(url);
                        response.EnsureSuccessStatusCode();
                        return await response.Content.ReadAsStringAsync();
                    }
                });

                while (!fetchTask.IsCompleted)
                {
                    yield return null;
                }

                if (fetchTask.IsFaulted)
                {
                    Debug.LogError("Failed to fetch permits: " + fetchTask.Exception?.Message);
                    if (loadingTrans != null) loadingTrans.GetComponent<TextMeshProUGUI>().text = "Error de red:\n" + fetchTask.Exception?.GetBaseException().Message;
                    yield break;
                }

                string json = fetchTask.Result;
                Debug.Log("Permit JSON Received: " + json);
                
                PermitData[] permits = null;
                try {
                    permits = JsonHelper.FromJson<PermitData>(json);
                } catch (System.Exception) {}

                // Si por algún motivo JsonUtility falla (suele pasar con arrays genéricos o fechas), extraemos manualmente:
                if (permits == null || permits.Length == 0 || string.IsNullOrEmpty(permits[0].pdfUrl))
                {
                    Debug.LogWarning("[PDF DEBUG] JsonUtility falló o devolvió vacío. Intentando extracción manual...");
                    int pdfIndex = json.IndexOf("\"pdfUrl\":\"", System.StringComparison.Ordinal);
                    if (pdfIndex > 0)
                    {
                        pdfIndex += 10; // longitud de "pdfUrl":""
                        int endQuote = json.IndexOf("\"", pdfIndex, System.StringComparison.Ordinal);
                        if (endQuote > pdfIndex)
                        {
                            _currentPdfUrl = json.Substring(pdfIndex, endQuote - pdfIndex);
                            Debug.Log($"[PDF DEBUG] Extracción manual exitosa: {_currentPdfUrl}");
                        }
                    }
                    
                    int imgIndex = json.IndexOf("\"previewImageUrl\":\"", System.StringComparison.Ordinal);
                    if (imgIndex > 0)
                    {
                        imgIndex += 19;
                        int endQuote = json.IndexOf("\"", imgIndex, System.StringComparison.Ordinal);
                        if (endQuote > imgIndex)
                        {
                            string imgUrl = json.Substring(imgIndex, endQuote - imgIndex);
                            if (permits == null || permits.Length == 0)
                            {
                                permits = new PermitData[] { new PermitData() { pdfUrl = _currentPdfUrl, previewImageUrl = imgUrl } };
                            }
                            else 
                            {
                                permits[0].previewImageUrl = imgUrl;
                            }
                        }
                    }
                }

                if (permits != null && permits.Length > 0)
                {
                    string imgUrl = permits[0].previewImageUrl;
                    
                    // Solamente sobreescribimos _currentPdfUrl si estaba vacío y si el permit tiene la URL
                    if (string.IsNullOrEmpty(_currentPdfUrl) && !string.IsNullOrEmpty(permits[0].pdfUrl))
                    {
                        _currentPdfUrl = permits[0].pdfUrl;
                    }
                    
#if !UNITY_EDITOR
                    if (!string.IsNullOrEmpty(_currentPdfUrl))
                    {
                        _currentPdfUrl = _currentPdfUrl.Replace("localhost", "192.168.178.57");
                    }
#endif
                    if (!string.IsNullOrEmpty(_currentPdfUrl))
                    {
                        _currentPdfUrl = System.Uri.EscapeUriString(_currentPdfUrl);
                        if (_openPdfBtnObj != null) _openPdfBtnObj.SetActive(true);
                    }

                    if (!string.IsNullOrEmpty(imgUrl))
                    {
                        // Fix localhost for headset testing
#if UNITY_EDITOR
                        // Do nothing, localhost is fine
#else
                        imgUrl = imgUrl.Replace("localhost", "192.168.178.57");
#endif
                        Debug.Log("Preview Image URL after replace: " + imgUrl);
                        
                        imgUrl = System.Uri.EscapeUriString(imgUrl);
                        StartCoroutine(LoadImageFromUrl(imgUrl, bgImage, loadingTrans));
                    }
                    else 
                    {
                        Debug.LogWarning("imgUrl is empty or null");
                        if (loadingTrans != null) loadingTrans.GetComponent<TextMeshProUGUI>().text = "No hay previsualización disponible";
                    }
                }
                else 
                {
                    Debug.LogWarning("Parsed permits array is empty or null.");
                    if (loadingTrans != null) loadingTrans.GetComponent<TextMeshProUGUI>().text = "No se encontraron permisos";
                }
            }
        }

        private GameObject CreatePermitPopup()
        {
            GameObject canvasObj = new GameObject("Permit_Popup");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            try { canvasObj.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>(); } catch { }

            if (Camera.main != null) canvas.worldCamera = Camera.main;

            RectTransform canvasRT = canvasObj.GetComponent<RectTransform>();
            canvasRT.sizeDelta = new Vector2(400, 620); // Suficiente espacio para la imagen y el botón
            canvasObj.transform.localScale = new Vector3(0.003f, 0.003f, 0.003f);

            // Background Image
            GameObject bgObj = new GameObject("PermitImage");
            bgObj.transform.SetParent(canvasObj.transform, false);
            RawImage bgImage = bgObj.AddComponent<RawImage>();
            // Use Meta dark background if texture not loaded yet
            bgImage.color = new Color(0.11f, 0.11f, 0.12f, 0.95f);
            
            RectTransform bgRT = bgObj.GetComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0.5f, 1); bgRT.anchorMax = new Vector2(0.5f, 1);
            bgRT.pivot = new Vector2(0.5f, 1);
            bgRT.sizeDelta = new Vector2(400, 560);
            bgRT.anchoredPosition = Vector2.zero;

            // Loading Text (bolita de cargando / texto)
            GameObject loadingObj = new GameObject("LoadingText");
            loadingObj.transform.SetParent(canvasObj.transform, false);
            TextMeshProUGUI loadingText = loadingObj.AddComponent<TextMeshProUGUI>();
            loadingText.text = "Cargando documento...\nPor favor espera";
            loadingText.fontSize = 22;
            loadingText.fontStyle = FontStyles.Bold;
            loadingText.alignment = TextAlignmentOptions.Center;
            loadingText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            RectTransform loadingRT = loadingObj.GetComponent<RectTransform>();
            loadingRT.anchorMin = new Vector2(0.5f, 0.5f); loadingRT.anchorMax = new Vector2(0.5f, 0.5f);
            loadingRT.pivot = new Vector2(0.5f, 0.5f);
            loadingRT.anchoredPosition = new Vector2(0, 50); // centrado sobre la imagen
            loadingRT.sizeDelta = new Vector2(350, 100);

            // Open PDF Button
            GameObject openBtnObj = new GameObject("OpenPdfButton");
            openBtnObj.transform.SetParent(canvasObj.transform, false);
            Image openBtnImg = openBtnObj.AddComponent<Image>();
            openBtnImg.sprite = GetPillSprite();
            openBtnImg.type = Image.Type.Sliced;
            openBtnImg.color = new Color(0f, 0.48f, 1f, 1f); // Meta Blue
            Button openBtn = openBtnObj.AddComponent<Button>();
            openBtnObj.SetActive(false); // <--- HIDE IT INITIALLY
            
            RectTransform openBtnRT = openBtnObj.GetComponent<RectTransform>();
            openBtnRT.anchorMin = new Vector2(0.5f, 0); openBtnRT.anchorMax = new Vector2(0.5f, 0);
            openBtnRT.pivot = new Vector2(0.5f, 0);
            openBtnRT.anchoredPosition = new Vector2(0, 60); // Encima del botón de cerrar
            openBtnRT.sizeDelta = new Vector2(150, 40);

            GameObject openBtnTextObj = new GameObject("OpenBtnText");
            openBtnTextObj.transform.SetParent(openBtnObj.transform, false);
            TextMeshProUGUI openBtnText = openBtnTextObj.AddComponent<TextMeshProUGUI>();
            openBtnText.text = "VER PDF";
            openBtnText.fontSize = 20;
            openBtnText.fontStyle = FontStyles.Bold;
            openBtnText.alignment = TextAlignmentOptions.Center;
            openBtnText.color = Color.white;
            RectTransform openBtnTextRT = openBtnTextObj.GetComponent<RectTransform>();
            openBtnTextRT.anchorMin = Vector2.zero; openBtnTextRT.anchorMax = Vector2.one;
            openBtnTextRT.sizeDelta = Vector2.zero;
            
            openBtn.onClick.AddListener(() => {
                openBtnText.text = "ABRIENDO...";
                Debug.Log($"[PDF DEBUG] VER PDF button clicked! URL is: {_currentPdfUrl}");
                if (!string.IsNullOrEmpty(_currentPdfUrl))
                {
                    Application.OpenURL(_currentPdfUrl);
                }
                else
                {
                    openBtnText.text = "URL VACÍA";
                }
            });

            // Almacenar referencia al botón para activarlo luego
            _openPdfBtnObj = openBtnObj;

            // Close Button
            GameObject btnObj = new GameObject("CloseButton");
            btnObj.transform.SetParent(canvasObj.transform, false);
            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.sprite = GetPillSprite();
            btnImg.type = Image.Type.Sliced;
            btnImg.color = new Color(0.17f, 0.17f, 0.18f, 1f); // Meta Gray
            Button btn = btnObj.AddComponent<Button>();
            RectTransform btnRT = btnObj.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.5f, 0); btnRT.anchorMax = new Vector2(0.5f, 0);
            btnRT.pivot = new Vector2(0.5f, 0);
            btnRT.anchoredPosition = new Vector2(0, 10); // Cerca de la base inferior
            btnRT.sizeDelta = new Vector2(150, 40);

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
            MakeGrabbable(canvasObj, canvasRT.sizeDelta);
            canvasObj.SetActive(false);
            return canvasObj;
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

        private IEnumerator LoadImageFromUrl(string url, RawImage rawImage, Transform loadingTrans = null)
        {
            Debug.Log("Downloading texture with HttpClient from: " + url);
            
            var fetchTask = System.Threading.Tasks.Task.Run(async () => {
                using (System.Net.Http.HttpClient client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = System.TimeSpan.FromSeconds(60);
                    var response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsByteArrayAsync();
                }
            });

            while (!fetchTask.IsCompleted)
            {
                yield return null;
            }

            if (fetchTask.IsFaulted)
            {
                Debug.LogError("Failed to download texture: " + fetchTask.Exception?.Message);
                if (loadingTrans != null) loadingTrans.GetComponent<TextMeshProUGUI>().text = "Error al descargar imagen:\n" + fetchTask.Exception?.GetBaseException().Message;
                yield break;
            }

            byte[] imageBytes = fetchTask.Result;
            if (imageBytes != null && imageBytes.Length > 0)
            {
                // Must be executed on the main thread
                Texture2D texture = new Texture2D(2, 2);
                if (texture.LoadImage(imageBytes))
                {
                    if (rawImage != null)
                    {
                        rawImage.texture = texture;
                        rawImage.color = Color.white;
                    }
                    Debug.Log("Texture applied successfully via HttpClient.");
                    if (loadingTrans != null) loadingTrans.gameObject.SetActive(false);
                }
                else
                {
                    Debug.LogError("Texture2D.LoadImage failed to parse bytes!");
                    if (loadingTrans != null) loadingTrans.GetComponent<TextMeshProUGUI>().text = "Error: Formato de imagen inválido";
                }
            }
            else
            {
                if (loadingTrans != null) loadingTrans.GetComponent<TextMeshProUGUI>().text = "Error: Imagen vacía";
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
            bgImage.sprite = GetRoundedSprite();
            bgImage.type = Image.Type.Sliced;
            bgImage.color = new Color(0.11f, 0.11f, 0.12f, 0.98f); // Meta Dark Gray
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
            btnImg.sprite = GetPillSprite();
            btnImg.type = Image.Type.Sliced;
            btnImg.color = new Color(0.17f, 0.17f, 0.18f, 1f); // Meta Gray
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
            MakeGrabbable(canvasObj, canvasRT.sizeDelta);
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
            if (_permitPopup != null)
            {
                _permitPopup.SetActive(false);
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
            canvasRT.sizeDelta = new Vector2(550, 220); // Más ancho, proporciones cinemáticas

            // 2. Main Background (Meta Glassmorphism oscuro)
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform, false);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.sprite = GetRoundedSprite();
            bgImage.type = Image.Type.Sliced;
            bgImage.color = new Color(0.11f, 0.11f, 0.12f, 0.95f); // Meta Dark Gray
            
            RectTransform bgRT = bgObj.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = Vector2.zero;

            // Drop Shadow falso para dar profundidad
            Shadow shadow = bgObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.5f);
            shadow.effectDistance = new Vector2(0, -5);

            // 3. Accent Line (Removido para estilo Meta, se mantiene el objeto apagado)
            GameObject accentObj = new GameObject("AccentLine");
            accentObj.transform.SetParent(bgObj.transform, false);
            Image accent = accentObj.AddComponent<Image>();
            accent.gameObject.SetActive(false); // Oculto

            // 4. Badge del Tipo (Pill shape background)
            GameObject badgeObj = new GameObject("BadgeBg");
            badgeObj.transform.SetParent(bgObj.transform, false);
            Image badge = badgeObj.AddComponent<Image>();
            badge.sprite = GetPillSprite();
            badge.type = Image.Type.Sliced;
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
            crewBtnImg.sprite = GetPillSprite();
            crewBtnImg.type = Image.Type.Sliced;
            crewBtnImg.color = new Color(0.17f, 0.17f, 0.18f, 1f); // Meta Gray
            Button cBtn = crewBtnObj.AddComponent<Button>();

            RectTransform cBtnRT = crewBtnObj.GetComponent<RectTransform>();
            cBtnRT.anchorMin = new Vector2(1, 0); cBtnRT.anchorMax = new Vector2(1, 0);
            cBtnRT.pivot = new Vector2(1, 0);
            cBtnRT.anchoredPosition = new Vector2(-280, 20); // a la izquierda del otro boton
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
            machBtnImg.sprite = GetPillSprite();
            machBtnImg.type = Image.Type.Sliced;
            machBtnImg.color = new Color(0.17f, 0.17f, 0.18f, 1f); // Meta Gray
            Button mBtn = machBtnObj.AddComponent<Button>();

            RectTransform mBtnRT = machBtnObj.GetComponent<RectTransform>();
            mBtnRT.anchorMin = new Vector2(1, 0); mBtnRT.anchorMax = new Vector2(1, 0);
            mBtnRT.pivot = new Vector2(1, 0);
            mBtnRT.anchoredPosition = new Vector2(-150, 20); // en medio
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

            // 9.5 Permit Button
            GameObject permitBtnObj = new GameObject("PermitButton");
            permitBtnObj.transform.SetParent(bgObj.transform, false);
            Image permitBtnImg = permitBtnObj.AddComponent<Image>();
            permitBtnImg.sprite = GetPillSprite();
            permitBtnImg.type = Image.Type.Sliced;
            permitBtnImg.color = new Color(0f, 0.48f, 1f, 1f); // Meta Blue
            Button pBtn = permitBtnObj.AddComponent<Button>();

            RectTransform pBtnRT = permitBtnObj.GetComponent<RectTransform>();
            pBtnRT.anchorMin = new Vector2(1, 0); pBtnRT.anchorMax = new Vector2(1, 0);
            pBtnRT.pivot = new Vector2(1, 0);
            pBtnRT.anchoredPosition = new Vector2(-20, 20); // a la derecha
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

            // 10. Close Button on main card
            GameObject closeBtnObj = new GameObject("CloseMainCardBtn");
            closeBtnObj.transform.SetParent(bgObj.transform, false);
            Image closeBtnImg = closeBtnObj.AddComponent<Image>();
            closeBtnImg.sprite = GetRoundedSprite();
            closeBtnImg.type = Image.Type.Sliced;
            closeBtnImg.color = new Color(0.17f, 0.17f, 0.18f, 1f); // Meta Gray
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

            // 11. Scale Button
            GameObject scaleBtnObj = new GameObject("ScaleMainCardBtn");
            scaleBtnObj.transform.SetParent(bgObj.transform, false);
            Image scaleBtnImg = scaleBtnObj.AddComponent<Image>();
            scaleBtnImg.sprite = GetRoundedSprite();
            scaleBtnImg.type = Image.Type.Sliced;
            scaleBtnImg.color = new Color(0.17f, 0.17f, 0.18f, 1f); // Meta Gray
            Button scBtn = scaleBtnObj.AddComponent<Button>();

            RectTransform scBtnRT = scaleBtnObj.GetComponent<RectTransform>();
            scBtnRT.anchorMin = new Vector2(1, 1); scBtnRT.anchorMax = new Vector2(1, 1);
            scBtnRT.pivot = new Vector2(1, 1);
            scBtnRT.anchoredPosition = new Vector2(-50, -10); // A la izquierda del botón de cerrar
            scBtnRT.sizeDelta = new Vector2(30, 30);

            GameObject scBtnTextObj = new GameObject("ScBtnText");
            scBtnTextObj.transform.SetParent(scaleBtnObj.transform, false);
            TextMeshProUGUI scBtnText = scBtnTextObj.AddComponent<TextMeshProUGUI>();
            scBtnText.text = "S"; // S de Scale o Size
            scBtnText.fontSize = 16;
            scBtnText.fontStyle = FontStyles.Bold;
            scBtnText.alignment = TextAlignmentOptions.Center;
            scBtnText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            RectTransform scBtnTextRT = scBtnTextObj.GetComponent<RectTransform>();
            scBtnTextRT.anchorMin = Vector2.zero; scBtnTextRT.anchorMax = Vector2.one;
            scBtnTextRT.sizeDelta = Vector2.zero;

            // 7. Ensamblar Script
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

            // IMPORTANTE PARA XR: Asignar al layer 'UI' (5) para que los rayos interactúen correctamente
            SetLayerRecursively(canvasObj, 5);
            MakeGrabbable(canvasObj, canvasRT.sizeDelta);

            canvasObj.SetActive(false);
            return uiScript;
        }
    }
}
