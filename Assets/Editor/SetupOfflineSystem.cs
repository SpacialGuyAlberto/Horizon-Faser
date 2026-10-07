using UnityEditor;
using UnityEngine;

public class SetupOfflineSystem
{
    [MenuItem("HorizonFaser/Activar SQLite (Solo después de instalar paquetes)")]
    public static void ActivateSQLite()
    {
        string definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
        if (!definesString.Contains("USE_SQLITE_READY"))
        {
            if (string.IsNullOrEmpty(definesString))
                definesString = "USE_SQLITE_READY";
            else
                definesString += ";USE_SQLITE_READY";
                
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, definesString);
            Debug.Log("[Horizon-Faser] ✅ Configurado USE_SQLITE_READY. El código Offline está activo.");
        }
        else
        {
            Debug.Log("[Horizon-Faser] SQLite ya estaba activado.");
        }
    }
    
    [MenuItem("HorizonFaser/Añadir SyncManager a la Escena Activa")]
    public static void AddSyncManagerToScene()
    {
        // Busca si ya hay un objeto de servicios
        var servicesObj = GameObject.Find("[SERVICES]");
        if (servicesObj == null)
        {
            servicesObj = new GameObject("[SERVICES]");
            Debug.Log("[Horizon-Faser] Creado GameObject [SERVICES].");
        }
        
        // Añade el SyncManager si no lo tiene
        if (servicesObj.GetComponent<HorizonFaser.Repositories.SyncManager>() == null)
        {
            servicesObj.AddComponent<HorizonFaser.Repositories.SyncManager>();
            Debug.Log("[Horizon-Faser] ✅ Añadido SyncManager al objeto [SERVICES].");
            
            // Marca la escena como modificada para que Unity te pida guardarla
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }
        else
        {
            Debug.Log("[Horizon-Faser] El SyncManager ya estaba presente en la escena.");
        }
    }
}
