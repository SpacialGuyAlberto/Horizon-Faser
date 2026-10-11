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
            col.size = new Vector3(size.x, size.y + 30f, 30f);
            // Colocamos el collider centrado verticalmente y apenas retirado en Z para no bloquear los clicks en botones
            col.center = new Vector3(0, 10f, 15f);
            
            Rigidbody rb = canvasObj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            
            try 
            {
                var type = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable, Unity.XR.Interaction.Toolkit")
                           ?? System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.XRGrabInteractable, Unity.XR.Interaction.Toolkit");
                
                if (type != null)
                {
                    var grabComponent = canvasObj.AddComponent(type);
                    type.GetProperty("throwOnDetach")?.SetValue(grabComponent, false);
                    type.GetProperty("useDynamicAttach")?.SetValue(grabComponent, true);
                    type.GetProperty("trackPosition")?.SetValue(grabComponent, true);
                    type.GetProperty("trackRotation")?.SetValue(grabComponent, true);
                    type.GetProperty("retainTransformParent")?.SetValue(grabComponent, true);

                    var movTypeProp = type.GetProperty("movementType");
                    if (movTypeProp != null)
                    {
                        try 
                        {
                            var instantVal = System.Enum.Parse(movTypeProp.PropertyType, "Instantaneous");
                            movTypeProp.SetValue(grabComponent, instantVal);
                        }
                        catch { }
                    }
                }
            } 
            catch (System.Exception ex)
            {
                Debug.LogWarning("[UIXRUtils] Error configurando XRGrabInteractable: " + ex.Message);
            }
        }
    }
}
