using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables; // XRI 3.x

namespace HorizonFaser.XR
{
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class HideableElement : MonoBehaviour
    {
        private XRSimpleInteractable _interactable;

        private void Awake()
        {
            _interactable = GetComponent<XRSimpleInteractable>();
            _interactable.selectEntered.AddListener(OnSelectEntered);
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            // Oculta (apaga) el objeto entero cuando el usuario lo selecciona con el rayo y aprieta el gatillo
            gameObject.SetActive(false);
            Debug.Log($"[HideableElement] Elemento {gameObject.name} ocultado por el usuario.");
        }

        private void OnDestroy()
        {
            if (_interactable != null)
            {
                _interactable.selectEntered.RemoveListener(OnSelectEntered);
            }
        }
    }
}
