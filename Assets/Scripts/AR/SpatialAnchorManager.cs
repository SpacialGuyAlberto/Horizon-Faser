using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace HorizonFaser.AR
{
    /// <summary>
    /// Manejador de anclajes espaciales en Unity (Equivalente a ASpatialAnchorManager en Unreal).
    /// Utiliza AR Foundation (ARAnchorManager).
    /// </summary>
    [RequireComponent(typeof(ARAnchorManager))]
    public class SpatialAnchorManager : MonoBehaviour
    {
        private ARAnchorManager _anchorManager;
        
        // Diccionario para mantener un registro en memoria de los anclajes creados con una etiqueta
        private Dictionary<string, ARAnchor> _trackedAnchors = new Dictionary<string, ARAnchor>();

        private void Awake()
        {
            _anchorManager = GetComponent<ARAnchorManager>();
            if (_anchorManager == null)
            {
                Debug.LogError("ARAnchorManager no encontrado en este GameObject.");
            }
        }

        /// <summary>
        /// Crea un anclaje en el mundo real en la posición y rotación dadas, 
        /// y opcionalmente ancla un objeto a este.
        /// (Equivalente a CreateAndSaveAnchor en Unreal)
        /// </summary>
        public ARAnchor CreateAndSaveAnchor(string anchorLabel, Pose worldPose, GameObject objectToPin = null)
        {
            // Creamos el anclaje espacial usando AR Foundation
            GameObject anchorGameObject = new GameObject($"ARAnchor_{anchorLabel}");
            anchorGameObject.transform.SetPositionAndRotation(worldPose.position, worldPose.rotation);
            
            ARAnchor newAnchor = anchorGameObject.AddComponent<ARAnchor>();

            if (newAnchor != null)
            {
                // Si pasamos un objeto (un GameObject/Componente), lo hacemos hijo del anclaje
                if (objectToPin != null)
                {
                    objectToPin.transform.SetParent(newAnchor.transform, false);
                    objectToPin.transform.localPosition = Vector3.zero;
                    objectToPin.transform.localRotation = Quaternion.identity;
                }

                _trackedAnchors[anchorLabel] = newAnchor;
                Debug.Log($"Anclaje '{anchorLabel}' guardado correctamente en la sesión actual.");

                // NOTA SOBRE PERSISTENCIA:
                // En Unreal usabas UARBlueprintLibrary::SaveARPinToLocalStore.
                // En Unity, guardar anclajes entre sesiones depende de tu plataforma:
                // - Meta Quest (OpenXR): Utiliza "XRAnchorStore" de OpenXR.
                // - ARCore (Android): Utiliza "ARCoreCloudAnchorManager" (Cloud Anchors) u OpenXR.
                // Aquí deberías añadir esa lógica si necesitas que persistan al cerrar la app.
            }
            else
            {
                Debug.LogWarning($"Error al crear el anclaje '{anchorLabel}'.");
                Destroy(anchorGameObject);
            }

            return newAnchor;
        }

        /// <summary>
        /// Carga los anclajes guardados.
        /// (Equivalente a LoadSavedAnchors en Unreal)
        /// </summary>
        public List<ARAnchor> LoadSavedAnchors()
        {
            var loadedPins = new List<ARAnchor>();
            
            // Aquí simularíamos la carga desde el almacenamiento local (ej. XRAnchorStore).
            // Por ahora, devolvemos los anclajes que están en memoria en esta sesión.
            foreach (var kvp in _trackedAnchors)
            {
                loadedPins.Add(kvp.Value);
                Debug.Log($"Anclaje cargado: {kvp.Key}");
            }

            return loadedPins;
        }

        /// <summary>
        /// Elimina un anclaje específico.
        /// (Equivalente a RemoveAnchor en Unreal)
        /// </summary>
        public void RemoveAnchor(string anchorLabel)
        {
            if (_trackedAnchors.TryGetValue(anchorLabel, out ARAnchor anchorToRemove))
            {
                // En AR Foundation, destruyendo el GameObject asociado al ARAnchor
                // se elimina automáticamente el anclaje del sistema XR subyacente.
                if (anchorToRemove != null)
                {
                    Destroy(anchorToRemove.gameObject);
                }
                
                _trackedAnchors.Remove(anchorLabel);
                Debug.Log($"Anclaje '{anchorLabel}' eliminado.");
            }
            else
            {
                Debug.LogWarning($"No se encontró el anclaje '{anchorLabel}' para eliminar.");
            }
        }
    }
}
