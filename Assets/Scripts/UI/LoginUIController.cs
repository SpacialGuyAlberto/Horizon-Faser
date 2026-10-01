using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using HorizonFaser.Repositories;
using System.Threading.Tasks;

namespace HorizonFaser.UI
{
    /// <summary>
    /// Controlador responsable únicamente de la lógica de autenticación y flujo de la UI de Login.
    /// Respeta el Principio de Responsabilidad Única (SRP).
    /// </summary>
    public class LoginUIController : MonoBehaviour
    {
        [Header("UI References")]
        public TMP_InputField emailInput;
        public TMP_InputField passwordInput;
        public Button loginButton;
        public TextMeshProUGUI statusText;

        [Header("Configuration")]
        public string serverUrl = "http://192.168.178.57:8080"; 
        public GameObject dashboardUI; 
        
        // Dependencia inyectada para el acceso a datos
        private QuarkBauRemoteRepository _repository;

        private void Awake()
        {
            // Restaurar el "hardcode" temporal porque el Inspector de Unity
            // puede tener guardada la URL antigua ("https://tu-servidor.com").
            serverUrl = "http://192.168.178.57:8080";

            _repository = new QuarkBauRemoteRepository();
            _repository.Configure(serverUrl, "");
        }

        private void Start()
        {
            // 1. Delegamos el estilo a una clase encargada solo de eso
            LoginUIThemeApplier.ApplyPremiumDarkStyle(this);
            
            // 2. Delegamos la creación del botón de salir
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                LoginUIThemeApplier.AddQuitButton(canvas.transform);
            }

            // 3. Posicionar el menú (Responsabilidad de la vista)
            PositionMenuInFrontOfCamera();

            // 4. Configurar eventos
            if (loginButton != null)
            {
                loginButton.onClick.AddListener(OnLoginClicked);
            }
        }

        private void PositionMenuInFrontOfCamera()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null) mainCam = FindObjectOfType<Camera>();

            if (mainCam != null)
            {
                transform.position = mainCam.transform.position + mainCam.transform.forward * 1.5f;
                transform.rotation = Quaternion.LookRotation(transform.position - mainCam.transform.position);
            }
            else
            {
                Debug.LogError("[LoginUI] No se encontró cámara en la escena.");
            }
        }

        private void Update()
        {
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

                var userInfo = await _repository.LoginAsync(email, pass);
                if (statusText != null) statusText.text = $"Bienvenido, {userInfo.FirstName}. Obteniendo proyectos...";
                
                var projects = await _repository.FetchProjectsAsync();

                if (projects != null && projects.Count > 0)
                {
                    if (statusText != null) statusText.text = "¡Sincronización completada!";
                    await Task.Delay(1500);

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
                Debug.LogError($"Login error: {ex.Message}");
                
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
