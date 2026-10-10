using NUnit.Framework;
using UnityEngine;
using HorizonFaser.XR;
using UnityEngine.InputSystem.XR;

namespace HorizonFaser.Tests
{
    public class XRBootstrapperTest
    {
        [Test]
        public void InitializeEnvironment_AddsTrackedPoseDriverToMainCamera()
        {
            // Arrange
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();

            // Act
            XRBootstrapper.InitializeEnvironment();

            // Assert
            var tpd = cam.GetComponent<TrackedPoseDriver>();
            Assert.IsNotNull(tpd, "El TrackedPoseDriver no fue añadido a la cámara principal.");
            
            Assert.IsNotNull(tpd.positionAction, "positionAction no fue configurado.");
            Assert.IsTrue(tpd.positionAction.bindings.Count > 0, "positionAction no tiene bindings.");
            Assert.AreEqual("<XRHMD>/centerEyePosition", tpd.positionAction.bindings[0].path);

            Assert.IsNotNull(tpd.rotationAction, "rotationAction no fue configurado.");
            Assert.IsTrue(tpd.rotationAction.bindings.Count > 0, "rotationAction no tiene bindings.");
            Assert.AreEqual("<XRHMD>/centerEyeRotation", tpd.rotationAction.bindings[0].path);

            // Cleanup
            Object.DestroyImmediate(camObj);
            
            // Clean up the dummy AR Session created by the bootstrapper
            var arSession = GameObject.Find("AR Session");
            if (arSession != null)
            {
                Object.DestroyImmediate(arSession);
            }
        }
    }
}
