using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;

namespace HorizonFaser.XR
{
    /// <summary>
    /// Se encarga de inicializar y parchear el entorno de Realidad Mixta (AR/XR) 
    /// automáticamente al cargar la escena, separando esta responsabilidad de la UI (SRP).
    /// </summary>
    public static class XRBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void InitializeEnvironment()
        {
            SetupARSession();
            SetupCamerasForPassthrough();
            CleanDummyEnvironment();
            PatchEventSystemForXR();
            PatchCanvasesForXR();
        }

        private static void SetupARSession()
        {
            if (UnityEngine.Object.FindObjectOfType<ARSession>() == null)
            {
                GameObject arSessionObj = new GameObject("AR Session");
                arSessionObj.AddComponent<ARSession>();
            }
        }

        private static void SetupCamerasForPassthrough()
        {
            Camera[] allCameras = UnityEngine.Object.FindObjectsOfType<Camera>();
            if (allCameras == null || allCameras.Length == 0) return;

            foreach (Camera cam in allCameras)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0, 0, 0, 0); 
                
                var urpCameraData = cam.GetComponent<UniversalAdditionalCameraData>();
                if (urpCameraData != null)
                {
                    urpCameraData.renderType = CameraRenderType.Base;
                    urpCameraData.renderPostProcessing = false;
                }

                var camSkybox = cam.GetComponent<Skybox>();
                if (camSkybox != null) UnityEngine.Object.Destroy(camSkybox);

                RenderSettings.skybox = null;

                if (cam == Camera.main || allCameras.Length == 1)
                {
                    if (cam.gameObject.GetComponent<ARCameraManager>() == null)
                        cam.gameObject.AddComponent<ARCameraManager>();

                    if (cam.GetComponent<ARCameraBackground>() == null)
                        cam.gameObject.AddComponent<ARCameraBackground>();
                }
            }
        }

        private static void CleanDummyEnvironment()
        {
            DestroyIfExists("Environment");
            DestroyIfExists("Room");
            DestroyIfExists("BackgroundPanel");
        }

        private static void DestroyIfExists(string name)
        {
            var obj = GameObject.Find(name);
            if (obj != null) UnityEngine.Object.Destroy(obj);
        }

        private static void PatchEventSystemForXR()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem != null)
            {
                var standaloneInput = eventSystem.GetComponent<StandaloneInputModule>();
                if (standaloneInput != null) UnityEngine.Object.Destroy(standaloneInput);

                if (eventSystem.GetComponent<XRUIInputModule>() == null)
                    eventSystem.gameObject.AddComponent<XRUIInputModule>();
            }
        }

        private static void PatchCanvasesForXR()
        {
            Canvas[] canvases = UnityEngine.Object.FindObjectsOfType<Canvas>();
            if (canvases == null) return;
            
            Camera mainCam = Camera.main;

            foreach (var canvas in canvases)
            {
                if (canvas.renderMode != RenderMode.WorldSpace)
                {
                    canvas.renderMode = RenderMode.WorldSpace;
                    if (mainCam != null) canvas.worldCamera = mainCam;
                }

                var oldRaycaster = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
                if (oldRaycaster != null && oldRaycaster.GetType() == typeof(UnityEngine.UI.GraphicRaycaster)) 
                {
                    UnityEngine.Object.Destroy(oldRaycaster);
                }

                if (canvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
                {
                    canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
                }
            }
        }
    }
}
