using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

namespace HorizonFaser.XR
{
    /// <summary>
    /// Sistema de selección personalizado (Imán Vectorial) que ignora por completo
    /// las limitaciones de físicas y distancia de Unity XR. Funciona midiendo el ángulo
    /// entre el mando y los nodos.
    /// </summary>
    public class XRPointerMagnet : MonoBehaviour
    {
        public static XRPointerMagnet Instance;

        [Header("Configuración del Imán")]
        public float maxAngle = 25f; // Grados de tolerancia para "apuntar vagamente"
        public float scaleMultiplier = 9f; // Agrandado masivo (9x) para visibilidad a larga distancia

        private List<NetworkNodeInteractable> _nodes = new List<NetworkNodeInteractable>();
        private Dictionary<NetworkNodeInteractable, Vector3> _originalScales = new Dictionary<NetworkNodeInteractable, Vector3>();

        private NetworkNodeInteractable _hoveredNode;
        private bool _wasTriggerPressed = false;

        private void Awake()
        {
            if (Instance == null) { Instance = this; }
            else { Destroy(gameObject); }
        }

        public void RegisterNode(NetworkNodeInteractable node)
        {
            if (!_nodes.Contains(node))
            {
                _nodes.Add(node);
                _originalScales[node] = node.transform.localScale;
            }
        }

        private void Update()
        {
            Transform pointer = GetActivePointer();
            if (pointer == null) return;

            NetworkNodeInteractable bestNode = null;
            // Usamos un sistema de puntuación. Menor puntuación es mejor.
            float bestScore = float.MaxValue;

            // 1. Encontrar el mejor nodo evaluando Ángulo + Distancia
            foreach (var node in _nodes)
            {
                if (node == null) continue;
                // Ignorar Segmentos (los tubos). El cálculo de ángulos hacia el centro de un tubo de 10km 
                // no tiene sentido matemático y confunde al imán.
                if (node.nodeType == NetworkNodeType.Segmento) continue;

                Vector3 toNode = node.transform.position - pointer.position;
                float distance = toNode.magnitude;
                float angle = Vector3.Angle(pointer.forward, toNode.normalized);

                // Ángulo máximo permitido (25 grados para que funcione bien de cerca)
                if (angle < 25f)
                {
                    // Fórmula mágica: El ángulo manda, pero a ángulos similares, gana el más cercano.
                    // Cada 100 metros añaden 0.5 a la puntuación del ángulo.
                    float score = angle + (distance * 0.005f);

                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestNode = node;
                    }
                }
            }

            _hoveredNode = bestNode;

            // 2. Animar el tamaño visualmente (Hover)
            foreach (var node in _nodes)
            {
                if (node == null) continue;
                Vector3 target = _originalScales[node];
                
                if (node == _hoveredNode)
                {
                    target = _originalScales[node] * scaleMultiplier;
                }
                
                // Suavizar el crecimiento
                node.transform.localScale = Vector3.Lerp(node.transform.localScale, target, Time.deltaTime * 15f);
            }

            // 3. Detectar el Clic (Gatillo de VR) puenteando el sistema XRI
            bool isPressed = false;
            var rightHandDevices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, rightHandDevices);
            if (rightHandDevices.Count > 0)
            {
                rightHandDevices[0].TryGetFeatureValue(CommonUsages.triggerButton, out isPressed);
            }

            // Fallbacks de clic eliminados (el viejo Input.GetKey causa errores si Unity está forzado a usar el Nuevo Input System)

            // 4. Disparar acción
            if (isPressed && !_wasTriggerPressed)
            {
                if (_hoveredNode != null)
                {
                    _hoveredNode.ForceTrigger();
                }
            }
            _wasTriggerPressed = isPressed;
        }

        private Transform GetActivePointer()
        {
            // Intentar encontrar el Controller derecho (Mando o Mano)
            var rays = UnityEngine.Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            
            Transform fallbackRay = null;
            foreach (var ray in rays)
            {
                string n = ray.gameObject.name.ToLower();
                if (n.Contains("right") || n.Contains("derecho") || n.Contains("righthand"))
                    return ray.transform;
                    
                fallbackRay = ray.transform;
            }
            
            // Si hay rayos pero no sabemos cuál es el derecho, devolvemos el último encontrado
            if (fallbackRay != null) return fallbackRay;

            // Si el usuario no tiene mandos encendidos, usar la dirección de su cabeza
            if (Camera.main != null) return Camera.main.transform;

            return null;
        }
    }
}
