using UnityEngine;

namespace HorizonFaser.UI
{
    public static class UIXRUtils
    {
        public static void SetLayerRecursively(GameObject obj, int newLayer)
        {
            if (null == obj) return;
            obj.layer = newLayer;
            foreach (Transform child in obj.transform)
            {
                if (null == child) continue;
                SetLayerRecursively(child.gameObject, newLayer);
            }
        }

        public static void MakeGrabbable(GameObject canvasObj, Vector2 size)
        {
            BoxCollider col = canvasObj.AddComponent<BoxCollider>();
            col.size = new Vector3(size.x, size.y, 40f); // 40f local thickness
            // Desplazamos el collider hacia atrás en Z para que no tape los botones de la UI
            col.center = new Vector3(0, 0, 30f);
            
            Rigidbody rb = canvasObj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            
            try 
            {
                var type = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable, Unity.XR.Interaction.Toolkit");
                if (type == null) type = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.XRGrabInteractable, Unity.XR.Interaction.Toolkit");
                
                if (type != null)
                {
                    var grabComponent = canvasObj.AddComponent(type);
                    type.GetProperty("throwOnDetach")?.SetValue(grabComponent, false);
                }
            } 
            catch { }
        }
    }
}
