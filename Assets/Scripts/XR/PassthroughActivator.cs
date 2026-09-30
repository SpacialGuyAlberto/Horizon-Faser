using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace HorizonFaser.XR
{
    /// <summary>
    /// Activa automaticamente el Passthrough de la Meta Quest 3.
    /// Adjuntar este script a cualquier GameObject en la escena.
    /// </summary>
    public class PassthroughActivator : MonoBehaviour
    {
        private void Start()
        {
            // 1. Configurar la camara principal para transparencia
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.clearFlags = CameraClearFlags.SolidColor;
                mainCam.backgroundColor = new Color(0f, 0f, 0f, 0f);
                Debug.Log("[Passthrough] Camara configurada como transparente.");
            }

            // 2. Activar ARCameraBackground si existe en la escena
            var arBg = FindObjectOfType<ARCameraBackground>();
            if (arBg != null)
            {
                arBg.enabled = true;
                Debug.Log("[Passthrough] ARCameraBackground activado.");
            }
            else if (mainCam != null)
            {
                // Si no existe, crearlo en la camara principal
                var arCam = mainCam.gameObject.GetComponent<ARCameraManager>();
                if (arCam == null) arCam = mainCam.gameObject.AddComponent<ARCameraManager>();
                
                if (mainCam.gameObject.GetComponent<ARCameraBackground>() == null)
                    mainCam.gameObject.AddComponent<ARCameraBackground>();
                
                Debug.Log("[Passthrough] ARCameraManager + ARCameraBackground anadidos dinamicamente.");
            }
        }
    }
}
