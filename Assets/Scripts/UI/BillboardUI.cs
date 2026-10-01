using UnityEngine;

public class BillboardUI : MonoBehaviour
{
    void LateUpdate()
    {
        if (Camera.main != null)
        {
            // Igualar la rotación de la cámara garantiza que la interfaz UI siempre sea visible y no se invierta
            transform.rotation = Camera.main.transform.rotation;
        }
    }
}
