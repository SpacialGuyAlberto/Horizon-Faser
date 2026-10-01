using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using HorizonFaser.Repositories;
using System.Threading.Tasks;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace HorizonFaser.UI
{
    public class LoginUIController : MonoBehaviour
    {
        [Header("UI References")]
        public TMP_InputField emailInput;
        public TMP_InputField passwordInput;
        public Button loginButton;
        public TextMeshProUGUI statusText;

        [Header("Configuration")]
        public string serverUrl = "http://192.168.178.57:8080"; // Debe ser tu IP local, NO localhost
        public GameObject dashboardUI; // Siguiente vista a mostrar
        private QuarkBauRemoteRepository _repository;

        private void Awake()
        {
            // Forzar la IP correcta aquí
            serverUrl = "http://192.168.178.57:8080";

            // 1. Crear AR Session ANTES que la cámara se configure
            if (FindObjectOfType<UnityEngine.XR.ARFoundation.ARSession>() == null)
            {
                GameObject arSessionObj = new GameObject("AR Session");
                arSessionObj.AddComponent<UnityEngine.XR.ARFoundation.ARSession>();
            }

            // --- APLICAR A TODAS LAS CÁMARAS ---
            Camera[] allCameras = FindObjectsOfType<Camera>();
            foreach (Camera cam in allCameras)
            {
                // Configurar la cámara a transparente
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0, 0, 0, 0); // Alpha 0 = transparente
                
                // Forzar URP a respetar la configuración base y evitar sobrescrituras de PostProcessing
                var urpCameraData = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                if (urpCameraData != null)
                {
                    urpCameraData.renderType = UnityEngine.Rendering.Universal.CameraRenderType.Base;
                    // En URP, el PostProcessing suele machacar el Alpha a 1. Lo desactivamos para UI/AR.
                    urpCameraData.renderPostProcessing = false;
                }

                // Destruir cualquier componente Skybox que esté forzando el cielo en esta cámara
                var camSkybox = cam.GetComponent<Skybox>();
                if (camSkybox != null) Destroy(camSkybox);

                // Destruir el cielo a nivel global
                RenderSettings.skybox = null;

                // 2. Añadir ARCameraManager y ARCameraBackground
                if (cam == Camera.main || allCameras.Length == 1)
                {
                    if (cam.gameObject.GetComponent<UnityEngine.XR.ARFoundation.ARCameraManager>() == null)
                        cam.gameObject.AddComponent<UnityEngine.XR.ARFoundation.ARCameraManager>();

                    // IMPORTANTE: En Meta OpenXR 2.6+, ARCameraBackground ES NECESARIO
                    // para notificar al compositor de Quest que debe encender el Passthrough.
                    if (cam.GetComponent<UnityEngine.XR.ARFoundation.ARCameraBackground>() == null)
                        cam.gameObject.AddComponent<UnityEngine.XR.ARFoundation.ARCameraBackground>();
                }
            }

            // --- DESTRUIR OBJETOS FALSOS DEL TEMPLATE QUE TAPAN LA VISTA ---
            // El "MR Template" de Unity pone una habitación falsa en 3D para probar en el PC.
            // Esto es literalmente una caja gris/azul gigante que tapa las gafas.
            var dummyEnv = GameObject.Find("Environment");
            if (dummyEnv != null) Destroy(dummyEnv);

            var dummyRoom = GameObject.Find("Room");
            if (dummyRoom != null) Destroy(dummyRoom);

            var bgPanel = GameObject.Find("BackgroundPanel");
            if (bgPanel != null) Destroy(bgPanel);
        }

        private void Start()
        {
            ApplyPremiumDarkStyle();
            
            Camera mainCam = Camera.main;
            if (mainCam == null) mainCam = FindObjectOfType<Camera>();

            if (mainCam != null)
            {
                // Colocar el menú JUSTO en frente de la cámara
                transform.position = mainCam.transform.position + mainCam.transform.forward * 1.5f;
                transform.rotation = Quaternion.LookRotation(transform.position - mainCam.transform.position);
            }
            else
            {
                Debug.LogError("[LoginUI] NO SE ENCONTRÓ NINGUNA CÁMARA EN LA ESCENA.");
            }

            // --- XR INTERACTION: Reemplazar GraphicRaycaster por el de XR para que los controladores Quest funcionen ---
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                // Asegurarse de que el Canvas está en World Space (necesario para XR)
                canvas.renderMode = RenderMode.WorldSpace;
                if (mainCam != null) canvas.worldCamera = mainCam;

                // Quitar el GraphicRaycaster normal
                var oldRaycaster = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
                if (oldRaycaster != null) Destroy(oldRaycaster);

                // Añadir el TrackedDeviceGraphicRaycaster del XR Interaction Toolkit
                if (canvas.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>() == null)
                    canvas.gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();
            }

            // -- FIX EVENT SYSTEM FOR XR CLICKS --
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem != null)
            {
                // Si existe el modulo Standalone, quitarlo y poner el XR
                var standaloneInput = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                if (standaloneInput != null) Destroy(standaloneInput);

                if (eventSystem.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule>() == null)
                    eventSystem.gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule>();
            }

            // Inicializar el repositorio
            _repository = new QuarkBauRemoteRepository();
            _repository.Configure(serverUrl, "");

            // Asignar el evento al botón de login
            if (loginButton != null)
            {
                loginButton.onClick.AddListener(OnLoginClicked);
            }

            // --- BOTÓN DE SALIR de la app ---
            AddQuitButton();
        }

        private void AddQuitButton()
        {
            // Crear un botón rojo de salida en la esquina superior derecha del Canvas
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            GameObject quitBtnObj = new GameObject("QuitButton");
            quitBtnObj.transform.SetParent(canvas.transform, false);

            var rt = quitBtnObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-20, -20);
            rt.sizeDelta = new Vector2(120, 50);

            var img = quitBtnObj.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(0.6f, 0.1f, 0.1f, 1f); // Rojo plano tipo Destructive Button

            var btn = quitBtnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => Application.Quit());

            // Texto del botón
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(quitBtnObj.transform, false);
            var textRt = textObj.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;
            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "✕ Salir";
            tmp.fontSize = 18;
            tmp.color = Color.white;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
        }

        private Sprite _proceduralRoundedSprite;
        private Sprite GetRoundedSprite()
        {
            if (_proceduralRoundedSprite != null) return _proceduralRoundedSprite;
            
            // Generar una textura circular perfecta (16px de radio) para usarla como 9-slice
            int r = 16;
            int size = r * 2;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color32 white = new Color32(255, 255, 255, 255);
            Color32 clear = new Color32(255, 255, 255, 0);
            Color32[] colors = new Color32[size * size];
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - r + 0.5f;
                    float dy = y - r + 0.5f;
                    // Distancia al centro. Pequeño suavizado (antialiasing)
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist <= r - 1f) colors[y * size + x] = white;
                    else if (dist <= r) colors[y * size + x] = new Color32(255, 255, 255, (byte)(255 * (r - dist)));
                    else colors[y * size + x] = clear;
                }
            }
            tex.SetPixels32(colors);
            tex.Apply();
            
            // Crear el sprite con bordes 9-slice (r, r, r, r)
            _proceduralRoundedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
            return _proceduralRoundedSprite;
        }

        private void ApplyPremiumDarkStyle()
        {
            // Estilo "Railway" con esquinas redondeadas y gradiente sólido
            Color32 bgTopColor = new Color32(31, 31, 34, 255);      
            Color32 bgBottomColor = new Color32(18, 18, 20, 255);   
            
            Color32 cardColor = new Color32(30, 30, 32, 255);     
            Color32 inputOutline = new Color32(63, 63, 70, 255);  
            
            Color32 buttonColor = new Color32(255, 107, 0, 255);  
            Color32 buttonText = new Color32(20, 20, 22, 255);    
            
            Color32 textWhite = new Color32(245, 245, 245, 255);  
            Color32 textMuted = new Color32(150, 150, 155, 255);  

            // Usamos nuestro generador infalible de bordes redondeados
            Sprite roundedSprite = GetRoundedSprite();

            // 1. Fondo del Panel (Sólido con Gradiente)
            Image panel = GetComponent<Image>();
            
            // ¡CRÍTICO! Si el objeto raíz no tiene Image (fondo transparente), le inyectamos uno.
            if (panel == null)
            {
                GameObject bgObj = new GameObject("SolidBackground");
                bgObj.transform.SetParent(this.transform, false);
                bgObj.transform.SetAsFirstSibling(); // Lo enviamos al fondo absoluto
                
                panel = bgObj.AddComponent<Image>();
                RectTransform rt = bgObj.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero; // Expandir a todo el Canvas
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = new Vector2(50, 50); // Un pequeño margen extra
            }

            if (panel != null)
            {
                panel.color = Color.white; // El gradiente dictará el color final
                panel.sprite = roundedSprite;
                panel.type = Image.Type.Sliced;
                panel.pixelsPerUnitMultiplier = 1f; 
                
                var outline = panel.gameObject.GetComponent<Outline>();
                if (outline != null) Destroy(outline); 

                // Añadir el gradiente vertical sólido
                var gradient = panel.gameObject.GetComponent<UIGradient>();
                if (gradient == null) gradient = panel.gameObject.AddComponent<UIGradient>();
                gradient.topColor = bgTopColor;
                gradient.bottomColor = bgBottomColor;
            }

            // 2. Inputs (Email y Password)
            TMP_InputField[] inputs = { emailInput, passwordInput };
            foreach (var input in inputs)
            {
                if (input != null && input.targetGraphic != null)
                {
                    Image inputImg = input.targetGraphic as Image;
                    if (inputImg != null)
                    {
                        inputImg.color = cardColor;
                        inputImg.sprite = roundedSprite;
                        inputImg.type = Image.Type.Sliced;
                        inputImg.pixelsPerUnitMultiplier = 1f; 
                    }
                    
                    var inputOutlineComp = input.targetGraphic.gameObject.GetComponent<Outline>();
                    if (inputOutlineComp == null) inputOutlineComp = input.targetGraphic.gameObject.AddComponent<Outline>();
                    inputOutlineComp.effectColor = inputOutline;
                    inputOutlineComp.effectDistance = new Vector2(1, -1); // Borde fino y definido

                    if (input.textComponent != null) input.textComponent.color = textWhite;
                    if (input.placeholder != null) input.placeholder.color = textMuted; 
                }
            }

            // 3. Botón de Entrar (Naranja Prominente)
            if (loginButton != null && loginButton.targetGraphic != null)
            {
                Image btnImg = loginButton.targetGraphic as Image;
                if (btnImg != null)
                {
                    btnImg.color = buttonColor; 
                    btnImg.sprite = roundedSprite;
                    btnImg.type = Image.Type.Sliced;
                    btnImg.pixelsPerUnitMultiplier = 1f; 
                }
                
                // Sin borde extra para el botón, ya que es sólido y brillante
                var btnOutline = loginButton.gameObject.GetComponent<Outline>();
                if (btnOutline != null) Destroy(btnOutline);

                var btnText = loginButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null) 
                {
                    btnText.color = buttonText;
                    btnText.fontStyle = FontStyles.Bold; // Resaltado e imponente
                }
            }

            // 4. Texto de estado
            if (statusText != null)
            {
                statusText.color = textMuted; 
            }
        }

        private void Update()
        {
            // Truco salvavidas: Poder iniciar sesión pulsando ENTER en el teclado
            // Como estás usando el nuevo Input System, leemos el teclado así:
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
            {
                OnLoginClicked();
            }
        }

        private async void OnLoginClicked()
        {
            if (statusText != null) statusText.text = "Conectando al Servidor...";
            if (loginButton != null) loginButton.interactable = false;

            try
            {
                string email = emailInput != null && !string.IsNullOrEmpty(emailInput.text) ? emailInput.text : "bauleiter@quarkbau.de";
                string pass = passwordInput != null && !string.IsNullOrEmpty(passwordInput.text) ? passwordInput.text : "password123";

                // 1. Iniciar sesión real
                var userInfo = await _repository.LoginAsync(email, pass);
                if (statusText != null) statusText.text = $"Bienvenido, {userInfo.FirstName}. Obteniendo proyectos...";
                Debug.Log($"[SYNC] Usuario logueado: {userInfo.Email}");

                // 2. Fetch Projects
                var projects = await _repository.FetchProjectsAsync();
                Debug.Log($"[SYNC] Proyectos descargados: {projects?.Count ?? 0}");

                if (projects != null && projects.Count > 0)
                {
                    if (statusText != null) statusText.text = "¡Sincronización completada!";
                    await Task.Delay(1500);

                    // Mostrar el Dashboard y pasarle los datos
                    if (dashboardUI != null) 
                    {
                        var dashboard = dashboardUI.GetComponent<DashboardUIController>();
                        if (dashboard != null)
                        {
                            dashboard.Setup(_repository, projects);
                        }
                        dashboardUI.SetActive(true);
                    }
                    gameObject.SetActive(false); 
                }
                else
                {
                    if (statusText != null) statusText.text = "No se encontraron proyectos.";
                }
            }
            catch (System.Exception ex)
            {
                if (statusText != null) statusText.text = "Error de Red: " + ex.Message;
                Debug.LogError($"Login/Fetch error: {ex.Message}");
                
                // Fallback a Modo Demo
                Debug.LogWarning("Fallback a Modo Demo porque el servidor falló.");
                await Task.Delay(1500);
                
                if (dashboardUI != null) dashboardUI.SetActive(true);
                gameObject.SetActive(false);
            }
            finally
            {
                if (loginButton != null) loginButton.interactable = true;
            }
        }
    }

    [AddComponentMenu("UI/Effects/Gradient")]
    public class UIGradient : UnityEngine.UI.BaseMeshEffect
    {
        public Color32 topColor = Color.white;
        public Color32 bottomColor = Color.black;

        public override void ModifyMesh(UnityEngine.UI.VertexHelper vh)
        {
            if (!IsActive()) return;

            var vertexList = new List<UIVertex>();
            vh.GetUIVertexStream(vertexList);
            int count = vertexList.Count;
            if (count == 0) return;

            float bottomY = vertexList[0].position.y;
            float topY = vertexList[0].position.y;

            for (int i = 1; i < count; i++)
            {
                float y = vertexList[i].position.y;
                if (y > topY) topY = y;
                else if (y < bottomY) bottomY = y;
            }

            float uiElementHeight = topY - bottomY;
            if (uiElementHeight <= 0) return;

            for (int i = 0; i < count; i++)
            {
                UIVertex uiVertex = vertexList[i];
                byte colorAlpha = uiVertex.color.a;
                Color32 color = Color32.Lerp(bottomColor, topColor, (uiVertex.position.y - bottomY) / uiElementHeight);
                color.a = (byte)((color.a * colorAlpha) / 255);
                uiVertex.color = color;
                vertexList[i] = uiVertex;
            }
            vh.Clear();
            vh.AddUIVertexTriangleStream(vertexList);
        }
    }
}
