using UnityEngine;

namespace HorizonFaser.UI
{
    public static class XRInteractionHelper
    {
        public static void MakeGrabbable(GameObject canvasObj, Vector2 size)
        {
            BoxCollider col = canvasObj.AddComponent<BoxCollider>();
            col.size = new Vector3(size.x, size.y, 50f); // 50f thickness to make it easily grabbable in VR
            
            Rigidbody rb = canvasObj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            
            try 
            {
                var grab = canvasObj.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
                grab.throwOnDetach = false;
            } 
            catch 
            {
                try 
                {
                    // Fallback to older XR Interaction Toolkit namespace
                    var type = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.XRGrabInteractable, Unity.XR.Interaction.Toolkit");
                    if (type != null)
                    {
                        var grabComponent = canvasObj.AddComponent(type);
                        type.GetProperty("throwOnDetach")?.SetValue(grabComponent, false);
                    }
                } catch { }
            }
        }

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
    }
}
