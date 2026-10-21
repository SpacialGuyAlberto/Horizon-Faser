using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables; // XRI 3.x
using HorizonFaser.UI;
using HorizonFaser.Models;

namespace HorizonFaser.XR
{
    public enum NetworkNodeType
    {
        Pop,
        Netzverteiler,
        Segmento,
        Utility
    }

    [RequireComponent(typeof(XRSimpleInteractable))]
    // ELIMINADO: [RequireComponent(typeof(BoxCollider))] para evitar crear cajas invisibles gigantes
    public class NetworkNodeInteractable : MonoBehaviour
    {
        [Header("Node Data")]
        public NetworkNodeType nodeType;
        public string nodeName = "Desconocido";
        public long segmentId;
        
        [TextArea]
        public string nodeDetails = "Detalles del nodo...";

        [Header("Advanced Data")]
        public Crew assignedCrew;
        public Machine assignedMachine;
        
        [Header("LineRenderer Integration (For Segments)")]
        public Vector3[] linePoints;
        public LineRenderer lineRenderer;

        [Header("UI Reference")]
        public NodeInfoCardUI infoCardUI;

        private XRSimpleInteractable _interactable;
        private static NodeInfoCardUI _sharedGeneratedCard;

        private void Awake()
        {
            _interactable = GetComponent<XRSimpleInteractable>();
            EnsureInfoCard();
        }

        private void EnsureInfoCard()
        {
            if (infoCardUI == null)
            {
                infoCardUI = NodeInfoCardUI.ActiveCard ?? FindFirstObjectByType<NodeInfoCardUI>(FindObjectsInactive.Include);
                if (infoCardUI == null)
                {
                    if (_sharedGeneratedCard == null)
                    {
                        _sharedGeneratedCard = NodeInfoCardUI.CreateGeneratedCard();
                    }
                    infoCardUI = _sharedGeneratedCard;
                }
            }
        }

        private void Start()
        {
            EnsureInfoCard();

            // Asegurarnos de que el Imán de Puntero exista siempre
            if (XRPointerMagnet.Instance == null)
            {
                var magnet = FindObjectOfType<XRPointerMagnet>();
                if (magnet == null)
                {
                    magnet = new GameObject("XRPointerMagnet").AddComponent<XRPointerMagnet>();
                }
                XRPointerMagnet.Instance = magnet;
            }
            
            // Registrar este nodo en el sistema de Puntero Magnético
            XRPointerMagnet.Instance.RegisterNode(this);
        }

        private void OnEnable()
        {
            if (_interactable != null)
            {
                _interactable.selectEntered.AddListener(OnXRSelected);
            }
        }

        private void OnDisable()
        {
            if (_interactable != null)
            {
                _interactable.selectEntered.RemoveListener(OnXRSelected);
            }
        }

        private void OnXRSelected(SelectEnterEventArgs args)
        {
            ShowInfoCard();
        }

        private void OnMouseDown()
        {
            ShowInfoCard();
        }

        private float _lastClickTime = 0f;
        private static GameObject _currentPermitQuad;

        private void ShowInfoCard()
        {
            float timeSinceLastClick = Time.time - _lastClickTime;
            
            // Ignorar rebotes rápidos de evento (< 0.15s)
            if (timeSinceLastClick < 0.15f) return;

            _lastClickTime = Time.time;

            EnsureInfoCard();

            // Si ya hay una tarjeta abierta para este MISMO nodo y se hace clic rápido (< 0.5s), cerrarla
            if (NodeInfoCardUI.IsAnyCardOpen && NodeInfoCardUI.ActiveCard != null && NodeInfoCardUI.ActiveCard.CurrentTargetNode == this.gameObject)
            {
                if (timeSinceLastClick < 0.5f)
                {
                    if (infoCardUI != null) infoCardUI.HideCard();
                    return;
                }
            }

            // Calcular posición espacial real: para segmentos usar el punto medio de su trayectoria
            Vector3 spawnPos = transform.position;
            if (linePoints != null && linePoints.Length > 0)
            {
                spawnPos = transform.TransformPoint(linePoints[linePoints.Length / 2]);
            }

            if (infoCardUI != null)
            {
                string typeStr = nodeType.ToString();
                infoCardUI.ShowCard(typeStr, nodeName, nodeDetails, spawnPos, assignedCrew, assignedMachine, segmentId, this.gameObject);
                Debug.Log($"[NetworkNode] Clic/Interacción detectado en: {typeStr} - {nodeName} (SegId: {segmentId})");
            }
            else
            {
                Debug.LogError("[NetworkNode] Error: infoCardUI no está disponible.");
            }
        }

        /// <summary>
        /// Expuesto para que sistemas personalizados de entrada (como XRPointerMagnet) 
        /// puedan forzar el evento de clic sin depender de las colisiones del motor.
        /// </summary>
        public void ForceTrigger()
        {
            ShowInfoCard();
        }

        /// <summary>
        /// Helper estático para adjuntar este comportamiento a cualquier GameObject puramente desde código.
        /// Útil cuando instancias tus nodos (POPs, Nvts, Segmentos) dinámicamente.
        /// </summary>
        public static NetworkNodeInteractable AttachToNode(GameObject targetObj, NetworkNodeType type, string name, string details, Crew crew = null, Machine machine = null, long segmentId = 0)
        {
            // PREPARACIÓN DE COLLIDERS:
            // Usamos BoxCollider porque en Android runtime MeshCollider falla en modelos importados
            // al estar marcados como non-accessible. BoxCollider es 100% fiable y súper ligero.
            if (type != NetworkNodeType.Segmento)
            {
                BoxCollider box = targetObj.GetComponent<BoxCollider>();
                if (box == null)
                {
                    box = targetObj.AddComponent<BoxCollider>();
                }

                Renderer[] renderers = targetObj.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    Bounds bounds = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++)
                    {
                        bounds.Encapsulate(renderers[i].bounds);
                    }

                    box.center = targetObj.transform.InverseTransformPoint(bounds.center);
                    Vector3 lossy = targetObj.transform.lossyScale;
                    Vector3 size = bounds.size;
                    box.size = new Vector3(
                        lossy.x != 0 ? Mathf.Abs(size.x / lossy.x) : size.x,
                        lossy.y != 0 ? Mathf.Abs(size.y / lossy.y) : size.y,
                        lossy.z != 0 ? Mathf.Abs(size.z / lossy.z) : size.z
                    );
                    // Asegurar un tamaño mínimo amplio para que sea fácil apuntar y hacer clic en VR
                    box.size = Vector3.Max(box.size, Vector3.one * 1.5f);
                }
                else
                {
                    box.size = Vector3.one * 2f;
                }
            }

            // AHORA añadimos el script interactivo (su Awake se ejecuta aquí)
            NetworkNodeInteractable interactable = targetObj.AddComponent<NetworkNodeInteractable>();
            interactable.nodeType = type;
            interactable.nodeName = name;
            interactable.nodeDetails = details;
            interactable.assignedCrew = crew;
            interactable.assignedMachine = machine;
            interactable.segmentId = segmentId;

            // Aseguramos que el XRSimpleInteractable registre TODOS los colliders (incluyendo hijos para segmentos)
            var xrInteractable = targetObj.GetComponent<XRSimpleInteractable>();
            if (xrInteractable != null)
            {
                var allColliders = targetObj.GetComponentsInChildren<Collider>();
                foreach (var c in allColliders)
                {
                    if (!xrInteractable.colliders.Contains(c))
                    {
                        xrInteractable.colliders.Add(c);
                    }
                }
            }

            return interactable;
        }
    }
}
