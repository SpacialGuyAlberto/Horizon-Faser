using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace HorizonFaser.UI
{
    public class DashboardUIController : MonoBehaviour
    {
        public TextMeshProUGUI welcomeText;
        public TextMeshProUGUI statsText;
        public Button openCesiumButton;
        public Button logoutButton;

        private void Start()
        {
            // Posicionar el Dashboard frente a la cámara (igual que el login)
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                transform.position = mainCam.transform.position + mainCam.transform.forward * 1.5f;
                transform.rotation = Quaternion.LookRotation(transform.position - mainCam.transform.position);
            }

            if (welcomeText != null) welcomeText.text = "¡Dashboard Principal de Horizon!";
            if (statsText != null) statsText.text = "Sistema sincronizado correctamente.\nTodo listo para cargar Cesium.";

            if (logoutButton != null)
            {
                logoutButton.onClick.AddListener(() =>
                {
                    // Recargar la escena actual para volver al login (básico)
                    UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                });
            }
            
            if (openCesiumButton != null)
            {
                openCesiumButton.onClick.AddListener(() =>
                {
                    if (statsText != null) statsText.text = "Cargando mapa 3D...";
                    // Aquí integraremos Cesium en el siguiente paso
                });
            }
        }
    }
}
