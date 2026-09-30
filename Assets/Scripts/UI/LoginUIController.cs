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
            // Forzar la IP correcta aquí para que Unity no use el valor viejo guardado en el Inspector (localhost)
            serverUrl = "http://192.168.178.57:8080";
        }

        private void Start()
        {
            ApplyTailwindAesthetics();
            
            // Colocar el menú JUSTO en frente de la cámara pase lo que pase
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                transform.position = mainCam.transform.position + mainCam.transform.forward * 1.5f;
                transform.rotation = Quaternion.LookRotation(transform.position - mainCam.transform.position);
            }

            // Inicializar el repositorio
            _repository = new QuarkBauRemoteRepository();
            _repository.Configure(serverUrl, "");

            // Asignar el evento al botón
            if (loginButton != null)
            {
                loginButton.onClick.AddListener(OnLoginClicked);
            }
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
                    long activeProjectId = projects[0].Id;
                    if (statusText != null) statusText.text = "Descargando Clusters y Segmentos...";

                    // 3. Fetch Clusters
                    var clusters = await _repository.FetchClusterByProjectIdAsync(activeProjectId);
                    Debug.Log($"[SYNC] Clusters descargados: {clusters?.Count ?? 0}");

                    // 4. Fetch Segments
                    var segments = await _repository.FetchProjectSegmentsByIdAsync(activeProjectId);
                    Debug.Log($"[SYNC] Segmentos descargados: {segments?.Count ?? 0}");
                }

                if (statusText != null) statusText.text = "¡Sincronización completada!";
                await Task.Delay(1500);
                
                // Mostrar el Dashboard y ocultar el Login
                if (dashboardUI != null) dashboardUI.SetActive(true);
                gameObject.SetActive(false); 
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
