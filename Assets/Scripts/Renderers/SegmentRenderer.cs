using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using HorizonFaser.Models;
using HorizonFaser.UI;
using CesiumForUnity;

namespace HorizonFaser.Renderers
{
    public static class SegmentRenderer
    {
        public static async Task RenderSegmentsCesium(List<QuarkBauSegment> segments, CanvasGroup cg, CesiumGeoreference georeference, HorizonFaser.Repositories.IFiberRepository repository)
        {
            if (cg != null) { cg.alpha = 0; cg.interactable = false; cg.blocksRaycasts = false; }
            
            if (georeference == null)
            {
                Debug.LogWarning("[Cesium] No se encontró CesiumGeoreference en la escena. Creando uno dinámicamente...");
                GameObject geoObj = new GameObject("CesiumGeoreference");
                georeference = geoObj.AddComponent<CesiumGeoreference>();
            }

            double minLat = double.MaxValue, maxLat = double.MinValue;
            double minLng = double.MaxValue, maxLng = double.MinValue;
            bool hasValidCoords = false;

            foreach (var seg in segments)
            {
                if (seg.Geometry != null)
                {
                    foreach (var pt in seg.Geometry)
                    {
                        if (pt.Latitude < minLat) minLat = pt.Latitude;
                        if (pt.Latitude > maxLat) maxLat = pt.Latitude;
                        if (pt.Longitude < minLng) minLng = pt.Longitude;
                        if (pt.Longitude > maxLng) maxLng = pt.Longitude;
                        hasValidCoords = true;
                    }
                }
            }

            double? dbElevation = null;
            foreach (var seg in segments)
            {
                if (seg.Geometry != null && seg.Geometry.Count > 0 && seg.Geometry[0].AltitudeZ.HasValue)
                {
                    dbElevation = seg.Geometry[0].AltitudeZ.Value;
                    break;
                }
            }

            // Intentar obtener las coordenadas desde el broadcast UDP del teléfono
            var companionListener = UnityEngine.Object.FindFirstObjectByType<HorizonFaser.Sensors.ARCompanionListener>();
            
            if (companionListener != null && companionListener.latestData != null && companionListener.latestData.lat != 0)
            {
                // Requerimiento: Usar la georreferencia del broadcast de la app en Android
                georeference.latitude = companionListener.latestData.lat;
                georeference.longitude = companionListener.latestData.lng;
                georeference.height = dbElevation.HasValue ? dbElevation.Value : (companionListener.latestData.alt > 0 ? companionListener.latestData.alt : 0);
                Debug.Log($"[Cesium] SegmentRenderer usa georreferencia del Teléfono (UDP): Lat {georeference.latitude}, Lng {georeference.longitude}, Alt {georeference.height}");
            }
            else if (hasValidCoords)
            {
                // Fallback: Si no hay teléfono conectado o no hay GPS, usar el centro de los segmentos
                georeference.longitude = (minLng + maxLng) / 2.0;
                georeference.latitude = (minLat + maxLat) / 2.0;
                georeference.height = dbElevation.HasValue ? dbElevation.Value : 0;
                Debug.LogWarning("[Cesium] SegmentRenderer usa georreferencia promediada (Fallback).");
            }

            await Task.Yield();

            GameObject segmentsParent = new GameObject("Cesium_Segments_Project6");
            segmentsParent.transform.SetParent(georeference.transform, false);

            Material segmentMaterial = new Material(Shader.Find("Unlit/Color"));
            segmentMaterial.color = new Color(1f, 0.5f, 0f, 1f); 

            int totalLines = 0;
            foreach (var segment in segments)
            {
                if (segment.Geometry == null || segment.Geometry.Count < 2) continue;

                // 1. Usar UN SOLO anchor temporal para la conversión matemática, en vez de crear miles.
                List<Vector3> controlPoints = new List<Vector3>();
                GameObject tempObj = new GameObject("TempCoordConverter");
                tempObj.transform.SetParent(segmentsParent.transform, false);
                var tempAnchor = tempObj.AddComponent<CesiumGlobeAnchor>();
                
                foreach (var p in segment.Geometry)
                {
                    tempAnchor.longitudeLatitudeHeight = new Unity.Mathematics.double3(p.Longitude, p.Latitude, p.AltitudeZ.HasValue ? p.AltitudeZ.Value : georeference.height);
                    controlPoints.Add(tempObj.transform.position); // World Space position
                }
                UnityEngine.Object.Destroy(tempObj);

                float totalInclination = 0f;
                float distance = 0f;
                for (int i = 0; i < controlPoints.Count - 1; i++)
                {
                    distance += Vector3.Distance(controlPoints[i], controlPoints[i+1]);
                    if (i < controlPoints.Count - 2)
                    {
                        Vector3 v1 = (controlPoints[i+1] - controlPoints[i]).normalized;
                        Vector3 v2 = (controlPoints[i+2] - controlPoints[i+1]).normalized;
                        totalInclination += Vector3.Angle(v1, v2);
                    }
                }

                // 2. Crear UN SOLO GameObject para TODO el Segmento
                GameObject segmentObj = new GameObject($"Segment_{segment.Id}");
                segmentObj.transform.SetParent(segmentsParent.transform, true);

                LineRenderer lineRenderer = segmentObj.AddComponent<LineRenderer>();
                lineRenderer.useWorldSpace = true; // controlPoints están en World Space
                lineRenderer.startWidth = 0.5f;
                lineRenderer.endWidth = 0.5f;
                lineRenderer.positionCount = controlPoints.Count;
                lineRenderer.SetPositions(controlPoints.ToArray());
                lineRenderer.alignment = LineAlignment.View;

                // 3. Material
                string basePath = "Models/Meshy_AI_corrugated_microduct/Meshy_AI_corrugated_microduct__1001094510_image-to-3d-texture";
                Material urpMat = PipeMaterialFactory.Get(basePath, segmentMaterial.color, 0.0f, 0.3f);
                if (urpMat != null)
                {
                    lineRenderer.material = urpMat;
                    lineRenderer.textureMode = LineTextureMode.Tile;
                }
                else
                {
                    lineRenderer.material = segmentMaterial;
                }

                // 4. Hitbox Ligero (Cajas separadas para evitar MeshColliders pesados)
                int colliderStep = Mathf.Max(1, controlPoints.Count / 10); 
                for (int i = 0; i < controlPoints.Count - 1; i += colliderStep)
                {
                    GameObject colObj = new GameObject("Hitbox");
                    colObj.transform.SetParent(segmentObj.transform, true);
                    colObj.transform.position = (controlPoints[i] + controlPoints[i+1]) / 2f;
                    colObj.transform.LookAt(controlPoints[i+1]);
                    BoxCollider bc = colObj.AddComponent<BoxCollider>();
                    bc.size = new Vector3(1f, 1f, Vector3.Distance(controlPoints[i], controlPoints[i+1]));
                }

                // 5. Interaction (Mocking)
                HorizonFaser.Models.Crew segmentCrew = null;
                HorizonFaser.Models.Machine segmentMachine = null;
                if (segment.AssignedCrewId.HasValue)
                {
                    segmentCrew = new HorizonFaser.Models.Crew
                    {
                        Id = segment.AssignedCrewId.Value,
                        Name = "Tiefbau Kolonne Alpha",
                        ForemanName = "Hans Mueller",
                        Size = 4,
                        VehiclePlate = "B-QB 1024"
                    };

                    segmentMachine = new HorizonFaser.Models.Machine
                    {
                        Id = 1,
                        SerialNumber = "BKH-993-X",
                        Name = "Caterpillar 301.8",
                        Type = "Mini Excavator",
                        Status = "IN_USE",
                        AssignedCrewId = segment.AssignedCrewId.Value
                    };
                    segmentCrew.AssignedMachines.Add(segmentMachine);
                }

                var interactable = HorizonFaser.XR.NetworkNodeInteractable.AttachToNode(
                    segmentObj, 
                    HorizonFaser.XR.NetworkNodeType.Segmento, 
                    $"Segmento {segment.Id}", 
                    $"Longitud: {distance:F1}m\nInclinación total: {totalInclination:F1}°",
                    segmentCrew,
                    segmentMachine,
                    segment.Id
                );
                
                // Añadir los datos necesarios para que el XRPointerMagnet detecte la línea matemáticamente
                interactable.linePoints = controlPoints.ToArray();
                interactable.lineRenderer = lineRenderer;

                // 6. Shovel Icon
                bool isExcavation = !string.IsNullOrEmpty(segment.WorkType) && 
                                  (segment.WorkType.IndexOf("Excavat", StringComparison.OrdinalIgnoreCase) >= 0 || 
                                   segment.WorkType.IndexOf("Trench", StringComparison.OrdinalIgnoreCase) >= 0);
                                   
                if (isExcavation && controlPoints.Count > 0) 
                {
                    Vector3 midPos = controlPoints[controlPoints.Count / 2];

                    GameObject shovelWrapper = new GameObject($"Shovel_{segment.Id}");
                    shovelWrapper.transform.SetParent(segmentsParent.transform, true);
                    shovelWrapper.transform.position = midPos + new Vector3(0, 30f, 0);
                    
                    GameObject shovelPrefab = Resources.Load<GameObject>("Models/ShovelIcon/Meshy_AI_macos_shovel_dirt_ico_1002155123_image-to-3d-texture");
                    if (shovelPrefab != null)
                    {
                        GameObject sVis = UnityEngine.Object.Instantiate(shovelPrefab);
                        sVis.transform.SetParent(shovelWrapper.transform, false);
                        sVis.transform.localScale = new Vector3(300f, 300f, 300f); 
                        
                        string shovelPath = "Models/ShovelIcon/Meshy_AI_macos_shovel_dirt_ico_1002155123_image-to-3d-texture";
                        Texture2D sTex = Resources.Load<Texture2D>(shovelPath);

                        Material sMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                        if (sTex != null) sMat.mainTexture = sTex;
                        
                        sMat.SetFloat("_AlphaClip", 1.0f);
                        sMat.EnableKeyword("_ALPHATEST_ON");
                        sMat.SetFloat("_Cutoff", 0.05f); 
                        
                        sMat.SetFloat("_Metallic", 0.0f);
                        sMat.SetFloat("_Smoothness", 0.1f);
                        
                        foreach (var r in sVis.GetComponentsInChildren<Renderer>())
                        {
                            r.material = sMat;
                        }
                        
                        shovelWrapper.AddComponent<IconRotator>();

                        var gazeScaler = sVis.AddComponent<GazeScaler>();
                        gazeScaler.baseScale = new Vector3(300f, 300f, 300f); 
                        gazeScaler.hoverScale = new Vector3(1000f, 1000f, 1000f); 
                    }
                }

                FencingRenderer.RenderSegmentFencingAsync(segment, segmentsParent, repository);
                CrewRenderer.RenderCrewOnSegment(segment, segmentsParent);
                
                totalLines++;
            }
            Debug.Log($"[Cesium] Segmentos renderizados con éxito: {totalLines} líneas (LineRenderer Optimizado).");

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.transform.position = new Vector3(0, 150f, -150f); 
                mainCam.transform.LookAt(Vector3.zero);
            }
        }
    }
}
