using UnityEditor;
using UnityEngine;
using HorizonFaser.XR;
using UnityEngine.InputSystem.XR;

public static class PrePushTestRunner
{
    public static void RunTests()
    {
        Debug.Log("======================================");
        Debug.Log("Running Pre-Push Tests...");
        Debug.Log("======================================");
        bool success = true;

        try
        {
            // Set up a mock camera
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();

            // Run the bootstrapper logic
            XRBootstrapper.InitializeEnvironment();

            // Verify the fix is applied correctly
            var tpd = cam.GetComponent<TrackedPoseDriver>();
            if (tpd == null) 
            { 
                Debug.LogError("TEST FAILED: TrackedPoseDriver was not added to the Main Camera."); 
                success = false; 
            }
            else 
            {
                if (tpd.positionAction == null || tpd.positionAction.bindings.Count == 0 || tpd.positionAction.bindings[0].path != "<XRHMD>/centerEyePosition") 
                { 
                    Debug.LogError("TEST FAILED: TrackedPoseDriver positionAction was not configured properly."); 
                    success = false; 
                }
                if (tpd.rotationAction == null || tpd.rotationAction.bindings.Count == 0 || tpd.rotationAction.bindings[0].path != "<XRHMD>/centerEyeRotation") 
                { 
                    Debug.LogError("TEST FAILED: TrackedPoseDriver rotationAction was not configured properly."); 
                    success = false; 
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("TEST FAILED WITH EXCEPTION: " + e.Message + "\n" + e.StackTrace);
            success = false;
        }

        Debug.Log("======================================");
        if (success)
        {
            Debug.Log("All tests passed! Ready to push.");
            EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError("Some tests failed! Push aborted.");
            EditorApplication.Exit(1);
        }
    }
}
