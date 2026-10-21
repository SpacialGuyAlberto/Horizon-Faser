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

        private bool _wasJoystickXPressed = false;
        private int _cycleOffset = 0;
        private NetworkNodeInteractable _lastBestNode = null;

        private void Update()
        {
            Transform pointer = GetActivePointer();
            if (pointer == null) return;

            // 1. Detectar Entradas (Gatillo y Joystick)
            bool isPressed = false;
            Vector2 joystick = Vector2.zero;
            var rightHandDevices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, rightHandDevices);
            if (rightHandDevices.Count > 0)
            {
                rightHandDevices[0].TryGetFeatureValue(CommonUsages.triggerButton, out isPressed);
                rightHandDevices[0].TryGetFeatureValue(CommonUsages.primary2DAxis, out joystick);
            }

            int flickDir = 0;
            if (joystick.x > 0.5f && !_wasJoystickXPressed) { flickDir = 1; _wasJoystickXPressed = true; }
            else if (joystick.x < -0.5f && !_wasJoystickXPressed) { flickDir = -1; _wasJoystickXPressed = true; }
            else if (Mathf.Abs(joystick.x) < 0.2f) { _wasJoystickXPressed = false; }

            // 2. Ciclado de opciones de la tarjeta con Joystick
            if (HorizonFaser.UI.NodeInfoCardUI.IsAnyCardOpen && HorizonFaser.UI.NodeInfoCardUI.ActiveCard != null)
            {
                if (flickDir != 0)
                {
                    HorizonFaser.UI.NodeInfoCardUI.ActiveCard.CycleOption(flickDir);
                }
            }

            // 3. Interacción con Nodos (con Ciclado por Joystick)
            List<NetworkNodeInteractable> validNodes = new List<NetworkNodeInteractable>();
            Dictionary<NetworkNodeInteractable, float> nodeScores = new Dictionary<NetworkNodeInteractable, float>();

            foreach (var node in _nodes)
            {
                if (node == null || !node.gameObject.activeInHierarchy) continue;

                Vector3 targetPoint = node.transform.position;

                if (node.nodeType == NetworkNodeType.Segmento)
                {
                    if (node.linePoints != null && node.linePoints.Length > 1)
                    {
                        float closestSubDist = float.MaxValue;
                        Vector3 bestPoint = node.linePoints[0];
                        for(int i = 0; i < node.linePoints.Length - 1; i++)
                        {
                            Vector3 pA = node.linePoints[i];
                            Vector3 pB = node.linePoints[i+1];
                            Vector3 closestOnSub = GetClosestPointOnSegmentToRay(pA, pB, pointer.position, pointer.forward);
                            float dist = Vector3.Distance(pointer.position, closestOnSub);
                            if(dist < closestSubDist)
                            {
                                closestSubDist = dist;
                                bestPoint = closestOnSub;
                            }
                        }
                        targetPoint = bestPoint;
                    }
                    else
                    {
                        float halfLength = node.transform.lossyScale.y;
                        Vector3 pA = node.transform.position - node.transform.up * halfLength;
                        Vector3 pB = node.transform.position + node.transform.up * halfLength;
                        targetPoint = GetClosestPointOnSegmentToRay(pA, pB, pointer.position, pointer.forward);
                    }
                }

                Vector3 toNode = targetPoint - pointer.position;
                float distance = toNode.magnitude;
                float angle = Vector3.Angle(pointer.forward, toNode.normalized);

                if (angle < maxAngle)
                {
                    float score = angle + (distance * 0.0001f);
                    if (node.nodeType == NetworkNodeType.Segmento && angle < 5f)
                    {
                        score = -1000f + angle; // Priorizar tubos si miramos directo a ellos
                    }
                    validNodes.Add(node);
                    nodeScores[node] = score;
                }
            }

            validNodes.Sort((a, b) => nodeScores[a].CompareTo(nodeScores[b]));

            NetworkNodeInteractable currentBest = validNodes.Count > 0 ? validNodes[0] : null;
            if (currentBest != _lastBestNode)
            {
                _cycleOffset = 0;
                _lastBestNode = currentBest;
            }

            if (validNodes.Count > 0)
            {
                if (flickDir != 0 && !HorizonFaser.UI.NodeInfoCardUI.IsAnyCardOpen)
                {
                    _cycleOffset += flickDir;
                }
                if (_cycleOffset < 0) _cycleOffset = validNodes.Count - 1;
                _hoveredNode = validNodes[_cycleOffset % validNodes.Count];
            }
            else
            {
                _hoveredNode = null;
            }

            if (isPressed && !_wasTriggerPressed)
            {
                if (_hoveredNode != null)
                {
                    _hoveredNode.ForceTrigger();
                }
                else if (HorizonFaser.UI.NodeInfoCardUI.IsAnyCardOpen && HorizonFaser.UI.NodeInfoCardUI.ActiveCard != null)
                {
                    HorizonFaser.UI.NodeInfoCardUI.ActiveCard.SelectCurrentOption();
                }
            }
            _wasTriggerPressed = isPressed;

            // 3. Animar el tamaño visualmente (Hover)
            foreach (var node in _nodes)
            {
                if (node == null) continue;
                Vector3 target = _originalScales[node];
                
                if (node == _hoveredNode)
                {
                    if (node.nodeType == NetworkNodeType.Segmento)
                    {
                        if (node.lineRenderer != null)
                        {
                            node.lineRenderer.widthMultiplier = Mathf.Lerp(node.lineRenderer.widthMultiplier, 3f, Time.deltaTime * 15f);
                        }
                        else
                        {
                            target.x = _originalScales[node].x * 50f;
                            target.z = _originalScales[node].z * 50f;
                        }
                    }
                    else
                    {
                        target = _originalScales[node] * scaleMultiplier;
                    }
                }
                else
                {
                    if (node.nodeType == NetworkNodeType.Segmento && node.lineRenderer != null)
                    {
                        node.lineRenderer.widthMultiplier = Mathf.Lerp(node.lineRenderer.widthMultiplier, 1f, Time.deltaTime * 15f);
                    }
                }
                
                if (node.nodeType != NetworkNodeType.Segmento || node.lineRenderer == null)
                {
                    node.transform.localScale = Vector3.Lerp(node.transform.localScale, target, Time.deltaTime * 15f);
                }
            }
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

        /// <summary>
        /// Calcula el punto exacto de un segmento 3D que pasa más cerca de un rayo infinito.
        /// Necesario para apuntar con láser a tubos largos.
        /// </summary>
        private Vector3 GetClosestPointOnSegmentToRay(Vector3 segA, Vector3 segB, Vector3 rayOrigin, Vector3 rayDir)
        {
            Vector3 u = segB - segA;
            Vector3 v = rayDir;
            Vector3 w = segA - rayOrigin;

            float a = Vector3.Dot(u, u);
            float b = Vector3.Dot(u, v);
            float c = Vector3.Dot(v, v);
            float d = Vector3.Dot(u, w);
            float e = Vector3.Dot(v, w);
            float D = a * c - b * b;
            float sc;

            if (D < 1e-6f)
            {
                sc = 0.0f;
            }
            else
            {
                sc = (b * e - c * d) / D;
            }

            sc = Mathf.Clamp01(sc);
            return segA + sc * u;
        }
    }
}
