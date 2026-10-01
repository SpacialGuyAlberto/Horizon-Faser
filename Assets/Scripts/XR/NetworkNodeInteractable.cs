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
    [RequireComponent(typeof(BoxCollider))]
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
            
            // Reparación Automática del Collider:
            // Si el Collider tiene tamaño 0,0,0, NUNCA funcionará el clic ni el rayo VR.
            BoxCollider col = GetComponent<BoxCollider>();
            if (col != null && col.size == Vector3.zero)
            {
                // Intentar ajustar el collider al tamaño del modelo 3D
                MeshRenderer mr = GetComponentInChildren<MeshRenderer>();
                if (mr != null)
                {
                    col.center = mr.transform.localPosition;
                    col.size = mr.bounds.size;
                }
                else
                {
                    col.size = Vector3.one * 0.5f; // Tamaño por defecto si no hay malla
                }
                Debug.Log($"[Fix] Collider ajustado en {gameObject.name}");
            }

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
            // Forzamos ocultar la tarjeta. 
            // Usamos un pequeño retraso (Invoke) para asegurar que si otro script la activó en Awake/Start, la volvamos a ocultar.
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
                // selectEntered es cuando haces CLIC (gatillo/trigger en VR)
                _interactable.selectEntered.AddListener(OnXRSelected);
                // Opcional: hoverEntered es cuando APUNTAS con el láser
                // _interactable.hoverEntered.AddListener(OnXRSelected); 
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
        /// Helper estático para adjuntar este comportamiento a cualquier GameObject puramente desde código.
        /// Útil cuando instancias tus nodos (POPs, Nvts, Segmentos) dinámicamente.
        /// </summary>
        public static NetworkNodeInteractable AttachToNode(GameObject targetObj, NetworkNodeType type, string name, string details)
        {
            NetworkNodeInteractable interactable = targetObj.AddComponent<NetworkNodeInteractable>();
            interactable.nodeType = type;
            interactable.nodeName = name;
            interactable.nodeDetails = details;
            
            // Ajustamos el BoxCollider por defecto para que sea un poco más grande si no hay uno configurado
            BoxCollider col = targetObj.GetComponent<BoxCollider>();
            if (col != null && col.size == Vector3.zero)
            {
                col.size = Vector3.one * 0.5f;
            }

            return interactable;
        }
    }
}
