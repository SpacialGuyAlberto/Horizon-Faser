using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables; // XRI 3.x
using HorizonFaser.UI;

namespace HorizonFaser.XR
{
    public enum NetworkNodeType
    {
        Pop,
        Netzverteiler,
        Segmento
    }

    [RequireComponent(typeof(XRSimpleInteractable))]
    // ELIMINADO: [RequireComponent(typeof(BoxCollider))] para evitar crear cajas invisibles gigantes
    public class NetworkNodeInteractable : MonoBehaviour
    {
        [Header("Node Data")]
        public NetworkNodeType nodeType;
        public string nodeName = "Desconocido";
        
        [TextArea]
        public string nodeDetails = "Detalles del nodo...";

        [Header("UI Reference")]
        public NodeInfoCardUI infoCardUI;

        private XRSimpleInteractable _interactable;
        private static NodeInfoCardUI _sharedGeneratedCard;

        private void Awake()
        {
            _interactable = GetComponent<XRSimpleInteractable>();
            
            // Gestión de la Interfaz UI
            if (infoCardUI == null)
            {
                infoCardUI = FindFirstObjectByType<NodeInfoCardUI>(FindObjectsInactive.Include);
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

            // Forzamos ocultar la tarjeta al iniciar
            Invoke(nameof(ForceHideUI), 0.1f);
        }

        private void ForceHideUI()
        {
            if (infoCardUI != null)
            {
                infoCardUI.HideCard();
            }
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

        private void ShowInfoCard()
        {
            if (infoCardUI != null)
            {
                string typeStr = nodeType.ToString();
                infoCardUI.ShowCard(typeStr, nodeName, nodeDetails, transform.position);
                Debug.Log($"[NetworkNode] Clic/Interacción detectado en: {typeStr} - {nodeName}");
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
        public static NetworkNodeInteractable AttachToNode(GameObject targetObj, NetworkNodeType type, string name, string details)
        {
            // PREPARACIÓN DE COLLIDERS:
            // Debe hacerse ANTES de añadir NetworkNodeInteractable, para que cuando 
            // XRSimpleInteractable ejecute su Awake(), los colisionadores ya existan y los detecte.
            if (type != NetworkNodeType.Segmento)
            {
                var oldBox = targetObj.GetComponent<BoxCollider>();
                if (oldBox != null) Destroy(oldBox);

                foreach (var mf in targetObj.GetComponentsInChildren<MeshFilter>())
                {
                    if (mf.gameObject.GetComponent<Collider>() == null)
                    {
                        var mc = mf.gameObject.AddComponent<MeshCollider>();
                        mc.convex = false; 
                    }
                }
            }

            // AHORA añadimos el script interactivo (su Awake se ejecuta aquí)
            NetworkNodeInteractable interactable = targetObj.AddComponent<NetworkNodeInteractable>();
            interactable.nodeType = type;
            interactable.nodeName = name;
            interactable.nodeDetails = details;

            return interactable;
        }
    }
}
