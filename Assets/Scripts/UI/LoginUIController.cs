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
            ApplyTailwindAesthetics();
            
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
            img.color = new Color(0.8f, 0.1f, 0.1f, 0.9f); // Rojo

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


        private void ApplyTailwindAesthetics()
        {
            // Fondos oscuros elegantes (Slate-900 / Slate-800)
            Color bgSlate900 = new Color(0.06f, 0.06f, 0.09f, 0.95f);
            Color bgSlate800 = new Color(0.12f, 0.13f, 0.18f, 1f);
            Color textWhite = new Color(0.95f, 0.95f, 0.95f, 1f);
            Color brandIndigo = new Color(0.39f, 0.22f, 0.94f, 1f); // bg-indigo-600

            // Estilizar el panel principal si tiene imagen
            Image panel = GetComponent<Image>();
            if (panel != null)
            {
                panel.color = bgSlate900;
            }

            // Estilizar Inputs
            TMP_InputField[] inputs = { emailInput, passwordInput };
            foreach (var input in inputs)
            {
                if (input != null && input.targetGraphic != null)
                {
                    input.targetGraphic.color = bgSlate800; // fondo de los inputs
                    if (input.textComponent != null) input.textComponent.color = textWhite;
                    if (input.placeholder != null) input.placeholder.color = new Color(0.5f, 0.5f, 0.6f, 1f); // gris tenue
                }
            }

            // Estilizar Botón de Login
            if (loginButton != null && loginButton.targetGraphic != null)
            {
                loginButton.targetGraphic.color = brandIndigo;
                var btnText = loginButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null) btnText.color = textWhite;
            }

            // Texto de estado
            if (statusText != null)
            {
                statusText.color = new Color(0.6f, 0.4f, 1f, 1f); // indigo claro
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
