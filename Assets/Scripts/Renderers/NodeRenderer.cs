using System.Collections.Generic;
using UnityEngine;
using HorizonFaser.Models;
using CesiumForUnity;

namespace HorizonFaser.Renderers
{
    public static class NodeRenderer
    {
        public static void RenderPopsAndNvtsCesium(List<Pop> pops, List<Netzverteiler> nvts, CesiumGeoreference georeference)
        {
            if (georeference == null) return;

            GameObject nodesParent = new GameObject("Cesium_Nodes_Project6");
            nodesParent.transform.SetParent(georeference.transform, false);

            Material popMaterial = new Material(Shader.Find("Unlit/Color"));
            popMaterial.color = new Color(1f, 0.4f, 0f, 1f); 

            Material nvtMaterial = new Material(Shader.Find("Unlit/Color"));
            nvtMaterial.color = new Color(1f, 1f, 0f, 1f); 

            int popCount = 0;
            int nvtCount = 0;

            if (pops != null)
            {
                foreach (var pop in pops)
                {
                    GameObject popPrefab = Resources.Load<GameObject>("Models/POP/Meshy_AI_pop_shelter_3d_1001095043_image-to-3d-texture");
                    GameObject popObj;
                    
                    if (popPrefab != null)
                    {
                        popObj = UnityEngine.Object.Instantiate(popPrefab);
                        popObj.name = $"POP_{pop.Id}";
                        popObj.transform.SetParent(nodesParent.transform, false);
                        popObj.transform.localScale = new Vector3(30f, 30f, 30f); 

                        string popBasePath = "Models/POP/Meshy_AI_pop_shelter_3d_1001095043_image-to-3d-texture";
                        Texture2D popTex = Resources.Load<Texture2D>(popBasePath);
                        Texture2D popNormalTex = Resources.Load<Texture2D>(popBasePath + "_normal");

                        Material popMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                        if (popTex != null) popMat.mainTexture = popTex;
                        else popMat.color = popMaterial.color;

                        if (popNormalTex != null)
                        {
                            popMat.SetTexture("_BumpMap", popNormalTex);
                            popMat.EnableKeyword("_NORMALMAP");
                        }
                        
                        popMat.SetFloat("_Metallic", 0.5f);
                        popMat.SetFloat("_Smoothness", 0.3f); 

                        foreach (var renderer in popObj.GetComponentsInChildren<Renderer>())
                        {
                            renderer.material = popMat;
                        }
                    }
                    else
                    {
                        popObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        popObj.name = $"POP_{pop.Id}_Fallback";
                        popObj.transform.SetParent(nodesParent.transform, false);
                        popObj.transform.localScale = new Vector3(8f, 8f, 8f);
                        popObj.GetComponent<MeshRenderer>().material = popMaterial;
                    }

                    string popId = pop.Id.ToString();
                    string popName = pop.Name;
                    HorizonFaser.XR.NetworkNodeInteractable.AttachToNode(
                        popObj, 
                        HorizonFaser.XR.NetworkNodeType.Pop, 
                        $"POP {popName}", 
                        $"ID: {popId}\nPos: {pop.Latitude:F5}, {pop.Longitude:F5}"
                    );

                    var popAnchor = popObj.AddComponent<CesiumGlobeAnchor>();
                    popAnchor.longitudeLatitudeHeight = new Unity.Mathematics.double3(pop.Longitude, pop.Latitude, 0);
                    popCount++;
                }
            }

            if (nvts != null)
            {
                foreach (var nvt in nvts)
                {
                    GameObject nvtPrefab = Resources.Load<GameObject>("Models/Meshy_Ai_street_cabinet/Meshy_AI_street_cabinet_3d_1001093310_image-to-3d-texture");
                    GameObject nvtObj;
                    
                    if (nvtPrefab != null)
                    {
                        nvtObj = UnityEngine.Object.Instantiate(nvtPrefab);
                        nvtObj.name = $"NVT_{nvt.Id}";
                        nvtObj.transform.SetParent(nodesParent.transform, false);
                        nvtObj.transform.localScale = new Vector3(30f, 30f, 30f); 

                        string nvtBasePath = "Models/Meshy_Ai_street_cabinet/Meshy_AI_street_cabinet_3d_1001093310_image-to-3d-texture";
                        Texture2D nvtTex = Resources.Load<Texture2D>(nvtBasePath);
                        Texture2D nvtNormalTex = Resources.Load<Texture2D>(nvtBasePath + "_normal");

                        Material urpMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                        if (nvtTex != null) urpMat.mainTexture = nvtTex;
                        else urpMat.color = Color.yellow;

                        if (nvtNormalTex != null)
                        {
                            urpMat.SetTexture("_BumpMap", nvtNormalTex);
                            urpMat.EnableKeyword("_NORMALMAP");
                        }
                        
                        urpMat.SetFloat("_Metallic", 0.6f); 
                        urpMat.SetFloat("_Smoothness", 0.4f);

                        foreach (var renderer in nvtObj.GetComponentsInChildren<Renderer>())
                        {
                            renderer.material = urpMat;
                        }
                    }
                    else
                    {
                        nvtObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        nvtObj.name = $"NVT_{nvt.Id}_Fallback";
                        nvtObj.transform.SetParent(nodesParent.transform, false);
                        nvtObj.transform.localScale = new Vector3(3f, 6f, 2f);
                        nvtObj.GetComponent<MeshRenderer>().material = nvtMaterial;
                    }

                    string nvtId = nvt.Id.ToString();
                    string nvtNameFixed = nvt.Identifier; 
                    HorizonFaser.XR.NetworkNodeInteractable.AttachToNode(
                        nvtObj, 
                        HorizonFaser.XR.NetworkNodeType.Netzverteiler, 
                        $"NVT {nvtNameFixed}", 
                        $"ID: {nvtId}\nPos: {nvt.Latitude:F5}, {nvt.Longitude:F5}"
                    );

                    var nvtAnchor = nvtObj.AddComponent<CesiumGlobeAnchor>();
                    nvtAnchor.longitudeLatitudeHeight = new Unity.Mathematics.double3(nvt.Longitude, nvt.Latitude, 0); 
                    nvtCount++;
                }
            }

            Debug.Log($"[Cesium] Nodos: {popCount} POPs y {nvtCount} NVTs.");
        }
    }
}
