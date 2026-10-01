using UnityEngine;
using UnityEngine.UI;
using TMPro; // Asegúrate de tener TextMeshPro importado en el proyecto
using HorizonFaser.Repositories;
using System.Threading.Tasks;

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
            ApplyMetaHorizonOSStyle();
            
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

        private void ApplyMetaHorizonOSStyle()
        {
            // Colores extraídos exactamente de la captura de pantalla de Meta Horizon OS
            Color panelBackground = new Color(0.10f, 0.11f, 0.13f, 1f); // Gris carbón muy oscuro y plano (opaco)
            Color primaryBlue = new Color(0.16f, 0.38f, 0.79f, 1f); // El azul "Primary Button" suave
            Color secondaryGrey = new Color(0.23f, 0.24f, 0.27f, 1f); // Gris "Secondary Button" (para inputs)
            Color textWhite = new Color(0.95f, 0.95f, 0.95f, 1f); // Blanco limpio

            // Cargar el sprite redondeado por defecto de Unity (para hacer el efecto "Pill/Bordes redondeados")
            Sprite roundedSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");

            // 1. Fondo principal plano (Sin bordes brillantes ni sombras)
            Image panel = GetComponent<Image>();
            if (panel != null)
            {
                panel.color = panelBackground;
                if (roundedSprite != null) 
                {
                    panel.sprite = roundedSprite;
                    panel.type = Image.Type.Sliced;
                }
                
                // Limpiar cualquier efecto viejo
                var outline = panel.gameObject.GetComponent<Outline>();
                if (outline != null) Destroy(outline);
            }

            // 2. Inputs usando el estilo "Secondary Button" de Meta
            TMP_InputField[] inputs = { emailInput, passwordInput };
            foreach (var input in inputs)
            {
                if (input != null && input.targetGraphic != null)
                {
                    Image inputImg = input.targetGraphic as Image;
                    if (inputImg != null)
                    {
                        inputImg.color = secondaryGrey;
                        if (roundedSprite != null)
                        {
                            inputImg.sprite = roundedSprite;
                            inputImg.type = Image.Type.Sliced;
                            inputImg.pixelsPerUnitMultiplier = 3f; // Multiplicador para redondear más los bordes
                        }
                    }
                    
                    var inputOutline = input.targetGraphic.gameObject.GetComponent<Outline>();
                    if (inputOutline != null) Destroy(inputOutline);

                    if (input.textComponent != null) input.textComponent.color = textWhite;
                    if (input.placeholder != null) input.placeholder.color = new Color(0.6f, 0.6f, 0.6f, 1f); 
                }
            }

            // 3. Botón de Login estilo "Primary Button" de Meta
            if (loginButton != null && loginButton.targetGraphic != null)
            {
                Image btnImg = loginButton.targetGraphic as Image;
                if (btnImg != null)
                {
                    btnImg.color = primaryBlue;
                    if (roundedSprite != null)
                    {
                        btnImg.sprite = roundedSprite;
                        btnImg.type = Image.Type.Sliced;
                        btnImg.pixelsPerUnitMultiplier = 2f; // Redondeo tipo Pill
                    }
                }
                
                // Los botones de Meta Horizon OS en esa pantalla son planos, sin sombra
                var btnShadow = loginButton.gameObject.GetComponent<Shadow>();
                if (btnShadow != null) Destroy(btnShadow);

                var btnText = loginButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null) 
                {
                    btnText.color = textWhite;
                    // No negrita pesada, la tipografía de Meta es bastante limpia
                    btnText.fontStyle = FontStyles.Normal;
                }
            }

            // Texto de estado
            if (statusText != null)
            {
                statusText.color = new Color(0.7f, 0.7f, 0.7f, 1f); 
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
}
