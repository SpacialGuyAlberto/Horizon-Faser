
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using CesiumForUnity;
using Unity.Mathematics;

namespace HorizonFaser.XR
{
    [RequireComponent(typeof(ARAnchorManager))]
    public class ARCalibrationManager : MonoBehaviour
    {
        public ARAnchorManager anchorManager;
        public CesiumGeoreference georeference;
        
        [Tooltip("The ID of the saved anchor, stored in PlayerPrefs")]
        private string savedAnchorIdKey = "HorizonFaser_SavedAnchorId";

        private ARAnchor currentCalibrationAnchor;

        void Awake()
        {
            if (anchorManager == null)
            {
                anchorManager = GetComponent<ARAnchorManager>();
            }
            if (georeference == null)
            {
                georeference = FindFirstObjectByType<CesiumGeoreference>();
            }
        }

        async void Start()
        {
            // Attempt to load the previously saved anchor on startup
            if (PlayerPrefs.HasKey(savedAnchorIdKey))
            {
                string anchorIdStr = PlayerPrefs.GetString(savedAnchorIdKey);
                if (Guid.TryParse(anchorIdStr, out Guid anchorId))
                {
                    Debug.Log($"[ARCalibrationManager] Intentando cargar el ancla guardada: {anchorId}");
                    await LoadAndApplyCalibrationAnchor(anchorId);
                }
            }
        }

        /// <summary>
        /// Realiza una calibración de un punto utilizando un GameObject virtual que sabemos dónde está.
        /// Mueve el CesiumGeoreference para que virtualObject coincida con realWorldPosition.
        /// </summary>
        public async Task CalibrateWithPoint(GameObject virtualObject, Vector3 realWorldPosition, Quaternion realWorldRotation)
        {
            if (georeference == null || virtualObject == null) return;

            // 1. Movemos el Georeference entero para alinear el objeto
            Vector3 offset = realWorldPosition - virtualObject.transform.position;
            georeference.transform.position += offset;

            // 2. Si queremos rotar el Georeference en base al objeto:
            // (Requiere un poco más de matemática para pivotar sobre el objeto, 
            // por ahora alineamos solo posición para simplificar)

            Debug.Log($"[ARCalibrationManager] Calibrado con punto. Offset aplicado: {offset}");

            // 3. Crear y guardar un ancla persistente en esta posición
            await CreateAndSavePersistentAnchor(realWorldPosition, realWorldRotation);
        }

        private async Task CreateAndSavePersistentAnchor(Vector3 position, Quaternion rotation)
        {
            if (anchorManager.subsystem == null)
            {
                Debug.LogWarning("[ARCalibrationManager] No hay subsystem de anchors disponible.");
                return;
            }

            // Remove previous anchor if it exists
            if (currentCalibrationAnchor != null)
            {
                if (anchorManager.subsystem.TryRemoveAnchor(currentCalibrationAnchor.trackableId))
                {
                    Destroy(currentCalibrationAnchor.gameObject);
                }
            }

            var pose = new Pose(position, rotation);
            
            // var anchor = await anchorManager.AddAnchorAsync(pose);
            // if (anchor == null)
            // {
            //     Debug.LogError("[ARCalibrationManager] Falló la creación del ancla.");
            //     return;
            // }
            // currentCalibrationAnchor = anchor;
            // var saveResult = await anchorManager.TrySaveAnchorAsync(anchor.trackableId);
            // if (saveResult.status.IsSuccess())
            // {
            //     Debug.Log($"[ARCalibrationManager] Ancla guardada exitosamente! ID: {anchor.trackableId}");
            //     PlayerPrefs.SetString(savedAnchorIdKey, anchor.trackableId.ToString());
            //     PlayerPrefs.Save();
            // }
            // else
            // {
            //     Debug.LogWarning($"[ARCalibrationManager] No se pudo guardar el ancla. Estado: {saveResult.status}");
            // }
        }

        private async Task LoadAndApplyCalibrationAnchor(Guid anchorId)
        {
            if (anchorManager.subsystem == null)
            {
                Debug.LogWarning("[ARCalibrationManager] Subsystem de anchors no disponible aún. No se puede cargar.");
                return;
            }

            // var loadResult = await anchorManager.TryLoadAnchorAsync(new TrackableId(anchorId.ToString()));
            // if (loadResult.status.IsSuccess() && loadResult.anchor != null)
            // {
            //     currentCalibrationAnchor = loadResult.anchor;
            //     Debug.Log($"[ARCalibrationManager] Ancla cargada en la posición: {currentCalibrationAnchor.transform.position}");
            // }
            // else
            // {
            //     Debug.LogWarning($"[ARCalibrationManager] Falló la carga del ancla. Estado: {loadResult.status}");
            // }
        }
    }
}
