using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace HorizonFaser.EditorTools
{
    public class CleanMRTemplate : Editor
    {
        [MenuItem("Tools/Horizon/Limpiar Errores de Plantilla")]
        public static void CleanTemplateScripts()
        {
            int removedCount = 0;
            
            // Buscar en todos los objetos de la escena actual
            GameObject[] allObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            
            foreach (GameObject go in allObjects)
            {
                removedCount += RemoveMRTemplateComponents(go);
            }
            
            Debug.Log($"¡Limpieza completada! Se eliminaron {removedCount} scripts problemáticos de la plantilla (GoalManager, SpawnedObjectsManager, etc).");
        }
        
        private static int RemoveMRTemplateComponents(GameObject obj)
        {
            int count = 0;
            
            // Buscar todos los componentes (incluidos los de los hijos)
            MonoBehaviour[] scripts = obj.GetComponentsInChildren<MonoBehaviour>(true);
            
            foreach (MonoBehaviour script in scripts)
            {
                if (script == null) continue;
                
                string name = script.GetType().Name;
                string ns = script.GetType().Namespace;
                
                if (ns == "UnityEngine.XR.Templates.MR" || 
                    name.Contains("GoalManager") || 
                    name.Contains("SpawnedObjectsManager") || 
                    name.Contains("OcclusionManager") ||
                    name.Contains("ARFeatureController"))
                {
                    Undo.DestroyObjectImmediate(script);
                    count++;
                }
            }
            
            return count;
        }
    }
}
