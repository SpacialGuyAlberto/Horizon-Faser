using System.Threading.Tasks;
using UnityEngine;
using HorizonFaser.Models;
using HorizonFaser.Repositories;
using CesiumForUnity;

namespace HorizonFaser.Renderers
{
    public static class FencingRenderer
    {
        public static async Task RenderSegmentFencingAsync(QuarkBauSegment segment, GameObject parent, IFiberRepository repository)
        {
            var plans = await repository.FetchFencingPlansAsync(segment.Id);
            double baseElevation = 0;
            if (segment.Geometry != null && segment.Geometry.Count > 0 && segment.Geometry[0].AltitudeZ.HasValue)
            {
                baseElevation = segment.Geometry[0].AltitudeZ.Value;
            }

            foreach (var plan in plans)
            {
                if (plan.elements == null) continue;
                foreach (var element in plan.elements)
                {
                    string resourcePath = "";
                    Vector3 scale = new Vector3(1000f, 1000f, 1000f); // Default scale (Cone)
                    
                    if (element.elementType == "CONE") 
                        resourcePath = "Models/Fencing/cono/Meshy_AI_traffic_cone_leitkege_1004105134_image-to-3d-texture_fbx/Meshy_AI_traffic_cone_leitkege_1004105134_image-to-3d-texture";
                    else if (element.elementType == "BARRIER")
                    {
                        resourcePath = "Models/Fencing/fence/Meshy_AI_construction_barrier__1004103924_image-to-3d-texture_fbx/Meshy_AI_construction_barrier__1004103924_image-to-3d-texture";
                        scale = new Vector3(1500f, 1500f, 1500f);
                    }
                    else if (element.elementType == "TRAFFIC_LIGHT" || element.elementType == "SIGN_STOP")
                    {
                        resourcePath = "Models/Fencing/traffic-light/Meshy_AI_construction_traffic__1004104811_image-to-3d-texture_fbx/Meshy_AI_construction_traffic__1004104811_image-to-3d-texture";
                        scale = new Vector3(3000f, 3000f, 3000f);
                    }

                    if (string.IsNullOrEmpty(resourcePath)) continue;

                    GameObject prefab = Resources.Load<GameObject>(resourcePath);
                    if (prefab != null)
                    {
                        GameObject fencingObj = UnityEngine.Object.Instantiate(prefab);
                        fencingObj.name = $"Fencing_{element.elementType}_{element.id}";
                        fencingObj.transform.SetParent(parent.transform, false);
                        fencingObj.transform.localScale = scale;

                        double finalLat = element.latitude;
                        double finalLng = element.longitude;

                        if (segment.Geometry != null && segment.Geometry.Count > 0)
                        {
                            double offsetLat = element.latitude - 52.5200;
                            double offsetLng = element.longitude - 13.4050;
                            
                            finalLat = segment.Geometry[0].Latitude + offsetLat;
                            finalLng = segment.Geometry[0].Longitude + offsetLng;
                        }

                        var anchor = fencingObj.AddComponent<CesiumForUnity.CesiumGlobeAnchor>();
                        anchor.longitudeLatitudeHeight = new Unity.Mathematics.double3(finalLng, finalLat, baseElevation + 1.0);
                        
                        Debug.Log($"[Fencing] Creado {element.elementType} en lat:{finalLat} lng:{finalLng} z:{baseElevation + 1.0}");

                        Texture2D tex = Resources.Load<Texture2D>(resourcePath);
                        Texture2D texNormal = Resources.Load<Texture2D>(resourcePath + "_normal");
                        Texture2D texMetallic = Resources.Load<Texture2D>(resourcePath + "_metallic");
                        
                        Material urpMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                        if (tex != null) urpMat.mainTexture = tex;
                        else urpMat.color = Color.yellow; // Fallback

                        if (texNormal != null)
                        {
                            urpMat.SetTexture("_BumpMap", texNormal);
                            urpMat.EnableKeyword("_NORMALMAP");
                        }
                        if (texMetallic != null)
                        {
                            urpMat.SetTexture("_MetallicGlossMap", texMetallic);
                            urpMat.SetFloat("_Metallic", 1f); // Use map
                            urpMat.SetFloat("_Smoothness", 0.5f);
                        }

                        int rendererCount = 0;
                        Bounds bounds = new Bounds();
                        bool hasBounds = false;

                        foreach (var renderer in fencingObj.GetComponentsInChildren<Renderer>())
                        {
                            renderer.material = urpMat;
                            rendererCount++;
                            if (!hasBounds)
                            {
                                bounds = renderer.bounds;
                                hasBounds = true;
                            }
                            else
                            {
                                bounds.Encapsulate(renderer.bounds);
                            }
                        }

                        Debug.Log($"[Fencing] {element.elementType} tiene {rendererCount} renderers. Tamaño real (bounds): {bounds.size.x} x {bounds.size.y} x {bounds.size.z}");

                        // Permitir al usuario ocultar las fences
                        foreach (var renderer in fencingObj.GetComponentsInChildren<Renderer>())
                        {
                            if (renderer.gameObject.GetComponent<Collider>() == null)
                            {
                                renderer.gameObject.AddComponent<MeshCollider>().convex = true;
                            }
                        }
                        
                        fencingObj.AddComponent<HorizonFaser.XR.HideableElement>();
                    }
                    else
                    {
                        Debug.LogError($"[Fencing] No se pudo cargar el prefab en la ruta: {resourcePath}");
                    }
                }
            }
        }
    }
}
