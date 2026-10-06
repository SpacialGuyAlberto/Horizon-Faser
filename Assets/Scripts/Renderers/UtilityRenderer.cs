using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using HorizonFaser.Models;
using HorizonFaser.UI;

namespace HorizonFaser.Renderers
{
    public static class UtilityRenderer
    {
        public static async Task RenderUtilitiesCesium(List<UtilityLine> utilities, CesiumForUnity.CesiumGeoreference georeference)
        {
            if (georeference == null) return;

            // Wait 1 frame to ensure coordinate initialization
            await Task.Yield();

            GameObject utilsParent = new GameObject("Cesium_Utilities_Water");
            utilsParent.transform.SetParent(georeference.transform, false);

            Material utilMaterial = new Material(Shader.Find("Unlit/Color"));
            utilMaterial.color = new Color(0f, 0.5f, 1f, 1f); // Blue for water

            int totalLines = 0;
            foreach (var utility in utilities)
            {
                if (utility.geometry == null || utility.geometry.Count < 2) continue;

                List<Vector3> tempPositions = new List<Vector3>();
                foreach (var p in utility.geometry)
                {
                    GameObject tempAnchor = new GameObject("TempAnchor");
                    tempAnchor.transform.SetParent(utilsParent.transform, false);
                    var globeAnchor = tempAnchor.AddComponent<CesiumForUnity.CesiumGlobeAnchor>();
                    globeAnchor.longitudeLatitudeHeight = new Unity.Mathematics.double3(p.Longitude, p.Latitude, 0);
                    tempPositions.Add(tempAnchor.transform.position);
                    UnityEngine.Object.Destroy(tempAnchor);
                }

                float totalInclination = 0f;
                float[] stepInclinations = new float[utility.geometry.Count - 1];
                
                for (int i = 0; i < tempPositions.Count - 2; i++)
                {
                    Vector3 v1 = (tempPositions[i+1] - tempPositions[i]).normalized;
                    Vector3 v2 = (tempPositions[i+2] - tempPositions[i+1]).normalized;
                    float angle = Vector3.Angle(v1, v2);
                    stepInclinations[i+1] = angle;
                    totalInclination += angle;
                }

                for (int i = 0; i < utility.geometry.Count - 1; i++)
                {
                    var pA = utility.geometry[i];
                    var pB = utility.geometry[i+1];
                    float currentInclination = stepInclinations[i];

                    GameObject anchorObjA = new GameObject($"Util_{utility.id}_A_{i}");
                    anchorObjA.transform.SetParent(utilsParent.transform, false);
                    var globeAnchorA = anchorObjA.AddComponent<CesiumForUnity.CesiumGlobeAnchor>();
                    globeAnchorA.longitudeLatitudeHeight = new Unity.Mathematics.double3(pA.Longitude, pA.Latitude, 0);

                    GameObject anchorObjB = new GameObject($"Util_{utility.id}_B_{i}");
                    anchorObjB.transform.SetParent(utilsParent.transform, false);
                    var globeAnchorB = anchorObjB.AddComponent<CesiumForUnity.CesiumGlobeAnchor>();
                    globeAnchorB.longitudeLatitudeHeight = new Unity.Mathematics.double3(pB.Longitude, pB.Latitude, 0);

                    Vector3 posA = anchorObjA.transform.position;
                    Vector3 posB = anchorObjB.transform.position;

                    GameObject cyl = new GameObject($"Utility_{utility.id}_Line_{i}");
                    cyl.transform.SetParent(utilsParent.transform, true);

                    GameObject prefabToLoad = null;
                    string basePath = "";
                    Color fallbackColor = utilMaterial.color;
                    
                    if (utility.utilityType == "ELECTRICITY")
                    {
                        basePath = "Models/Electricidad/Meshy_AI_holographic_conduit_3_1004094034_image-to-3d-texture_fbx/Meshy_AI_holographic_conduit_3_1004094034_image-to-3d-texture";
                        prefabToLoad = Resources.Load<GameObject>(basePath);
                        fallbackColor = new Color(1f, 0f, 0f, 1f); // Red for electricity
                    }
                    else if (utility.utilityType == "GAS")
                    {
                        basePath = "Models/Gas/Meshy_AI_long_toxic_green_pipe_1004101047_image-to-3d-texture_fbx/Meshy_AI_long_toxic_green_pipe_1004101047_image-to-3d-texture";
                        prefabToLoad = Resources.Load<GameObject>(basePath);
                        fallbackColor = new Color(0f, 1f, 0f, 1f); // Toxic Green for gas
                    }
                    else
                    {
                        basePath = "Models/Water/Meshy_AI_holographic_water_pip_1002204952_image-to-3d-texture_fbx/Meshy_AI_holographic_water_pip_1002204952_image-to-3d-texture";
                        prefabToLoad = Resources.Load<GameObject>(basePath);
                        fallbackColor = new Color(0f, 0.5f, 1f, 1f); // Blue for water
                    }
                    
                    GameObject utilVisual;
                    
                    if (prefabToLoad != null)
                    {
                        utilVisual = UnityEngine.Object.Instantiate(prefabToLoad);
                        utilVisual.transform.SetParent(cyl.transform, false);
                        
                        MeshFilter mf = utilVisual.GetComponentInChildren<MeshFilter>();
                        if (mf != null && mf.sharedMesh != null)
                        {
                            Vector3 boundsSize = mf.sharedMesh.bounds.size;
                            float maxAxis = Mathf.Max(boundsSize.x, boundsSize.y, boundsSize.z);
                            
                            if (maxAxis == boundsSize.x)
                            {
                                utilVisual.transform.localRotation = Quaternion.Euler(0, 0, 90);
                                float scaleModifier = 2f / boundsSize.x;
                                utilVisual.transform.localScale = new Vector3(scaleModifier, scaleModifier, scaleModifier);
                            }
                            else if (maxAxis == boundsSize.z)
                            {
                                utilVisual.transform.localRotation = Quaternion.Euler(90, 0, 0);
                                float scaleModifier = 2f / boundsSize.z;
                                utilVisual.transform.localScale = new Vector3(scaleModifier, scaleModifier, scaleModifier);
                            }
                            else
                            {
                                float scaleModifier = 2f / boundsSize.y;
                                utilVisual.transform.localScale = new Vector3(scaleModifier, scaleModifier, scaleModifier);
                            }
                        }

                        // Material compartido y anti-parpadeo (antes smoothness 0.8 => destellos)
                        Material urpMat = PipeMaterialFactory.Get(basePath, fallbackColor, 0.2f, 0.35f, useMetallicMap: true);

                        foreach (var r in utilVisual.GetComponentsInChildren<Renderer>())
                        {
                            r.sharedMaterial = urpMat;
                        }
                    }
                    else
                    {
                        utilVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        utilVisual.transform.SetParent(cyl.transform, false);
                        utilVisual.GetComponent<MeshRenderer>().material = utilMaterial;
                    }

                    BoxCollider box = cyl.AddComponent<BoxCollider>();
                    box.size = new Vector3(1f, 2f, 1f); 

                    var connector = cyl.AddComponent<SegmentConnector>();
                    connector.PointA = anchorObjA.transform;
                    connector.PointB = anchorObjB.transform;
                    connector.Thickness = 0.6f;
                    
                    HorizonFaser.XR.NetworkNodeInteractable.AttachToNode(
                        cyl, 
                        HorizonFaser.XR.NetworkNodeType.Segmento, 
                        $"Utility {utility.utilityType}", 
                        $"Operador: {utility.operatorName}\nID: {utility.id}"
                    );
                    
                    totalLines++;
                }
            }
            Debug.Log($"[Cesium] Utilities renderizados con éxito: {totalLines} líneas de agua.");
        }
    }
}
