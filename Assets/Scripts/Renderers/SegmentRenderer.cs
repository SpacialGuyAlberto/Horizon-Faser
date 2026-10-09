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

                List<Vector3> tempPositions = new List<Vector3>();
                foreach (var p in segment.Geometry)
                {
                    GameObject tempAnchor = new GameObject("TempAnchor");
                    tempAnchor.transform.SetParent(segmentsParent.transform, false);
                    var globeAnchor = tempAnchor.AddComponent<CesiumGlobeAnchor>();
                    globeAnchor.longitudeLatitudeHeight = new Unity.Mathematics.double3(p.Longitude, p.Latitude, p.AltitudeZ.HasValue ? p.AltitudeZ.Value : georeference.height);
                    tempPositions.Add(tempAnchor.transform.position);
                    UnityEngine.Object.Destroy(tempAnchor);
                }

                float totalInclination = 0f;
                float[] stepInclinations = new float[segment.Geometry.Count - 1];
                
                for (int i = 0; i < tempPositions.Count - 2; i++)
                {
                    Vector3 v1 = (tempPositions[i+1] - tempPositions[i]).normalized;
                    Vector3 v2 = (tempPositions[i+2] - tempPositions[i+1]).normalized;
                    float angle = Vector3.Angle(v1, v2);
                    stepInclinations[i+1] = angle;
                    totalInclination += angle;
                }

                for (int i = 0; i < segment.Geometry.Count - 1; i++)
                {
                    var pA = segment.Geometry[i];
                    var pB = segment.Geometry[i+1];
                    float currentInclination = stepInclinations[i];

                    GameObject anchorObjA = new GameObject($"Seg_{segment.Id}_A_{i}");
                    anchorObjA.transform.SetParent(segmentsParent.transform, false);
                    var globeAnchorA = anchorObjA.AddComponent<CesiumGlobeAnchor>();
                    globeAnchorA.longitudeLatitudeHeight = new Unity.Mathematics.double3(pA.Longitude, pA.Latitude, pA.AltitudeZ.HasValue ? pA.AltitudeZ.Value : georeference.height);

                    GameObject anchorObjB = new GameObject($"Seg_{segment.Id}_B_{i}");
                    anchorObjB.transform.SetParent(segmentsParent.transform, false);
                    var globeAnchorB = anchorObjB.AddComponent<CesiumGlobeAnchor>();
                    globeAnchorB.longitudeLatitudeHeight = new Unity.Mathematics.double3(pB.Longitude, pB.Latitude, pB.AltitudeZ.HasValue ? pB.AltitudeZ.Value : georeference.height);

                    Vector3 posA = anchorObjA.transform.position;
                    Vector3 posB = anchorObjB.transform.position;

                    Vector3 centerPos = (posA + posB) / 2f;
                    float distance = Vector3.Distance(posA, posB);
                    Vector3 direction = (posB - posA).normalized;

                    GameObject cyl = new GameObject($"Segment_{segment.Id}_Line_{i}");
                    cyl.transform.SetParent(segmentsParent.transform, true);

                    GameObject ductPrefab = Resources.Load<GameObject>("Models/Meshy_AI_corrugated_microduct/Meshy_AI_corrugated_microduct__1001094510_image-to-3d-texture");
                    GameObject ductVisual;
                    
                    if (ductPrefab != null)
                    {
                        ductVisual = UnityEngine.Object.Instantiate(ductPrefab);
                        ductVisual.transform.SetParent(cyl.transform, false);
                        
                        MeshFilter mf = ductVisual.GetComponentInChildren<MeshFilter>();
                        if (mf != null && mf.sharedMesh != null)
                        {
                            Vector3 boundsSize = mf.sharedMesh.bounds.size;
                            float maxAxis = Mathf.Max(boundsSize.x, boundsSize.y, boundsSize.z);
                            
                            if (maxAxis == boundsSize.x)
                            {
                                ductVisual.transform.localRotation = Quaternion.Euler(0, 0, 90);
                                float scaleModifier = 2f / boundsSize.x;
                                ductVisual.transform.localScale = new Vector3(scaleModifier, scaleModifier, scaleModifier);
                            }
                            else if (maxAxis == boundsSize.z)
                            {
                                ductVisual.transform.localRotation = Quaternion.Euler(90, 0, 0);
                                float scaleModifier = 2f / boundsSize.z;
                                ductVisual.transform.localScale = new Vector3(scaleModifier, scaleModifier, scaleModifier);
                            }
                            else
                            {
                                float scaleModifier = 2f / boundsSize.y;
                                ductVisual.transform.localScale = new Vector3(scaleModifier, scaleModifier, scaleModifier);
                            }
                        }

                        string basePath = "Models/Meshy_AI_corrugated_microduct/Meshy_AI_corrugated_microduct__1001094510_image-to-3d-texture";
                        // Material compartido y anti-parpadeo (smoothness baja = sin destellos especulares)
                        Material urpMat = PipeMaterialFactory.Get(basePath, segmentMaterial.color, 0.0f, 0.3f);

                        foreach (var r in ductVisual.GetComponentsInChildren<Renderer>())
                        {
                            r.sharedMaterial = urpMat;
                        }
                    }
                    else
                    {
                        ductVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        ductVisual.transform.SetParent(cyl.transform, false);
                        ductVisual.GetComponent<MeshRenderer>().material = segmentMaterial;
                    }

                    BoxCollider box = cyl.AddComponent<BoxCollider>();
                    box.size = new Vector3(1f, 2f, 1f); 
                    
                    var connector = cyl.AddComponent<SegmentConnector>();
                    connector.PointA = anchorObjA.transform;
                    connector.PointB = anchorObjB.transform;
                    connector.Thickness = 0.5f; 
                    
                    string inclinacionStr = (i == 0) ? "N/A (Inicio)" : $"{currentInclination:F1}°";
                    
                    HorizonFaser.Models.Crew segmentCrew = null;
                    HorizonFaser.Models.Machine segmentMachine = null;

                    if (segment.AssignedCrewId.HasValue)
                    {
                        // Mocking Crew and Machine data for demonstration since we only have the ID
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

                    HorizonFaser.XR.NetworkNodeInteractable.AttachToNode(
                        cyl, 
                        HorizonFaser.XR.NetworkNodeType.Segmento, 
                        $"Segmento {segment.Id}", 
                        $"Longitud: {distance:F1}m\nInclinación tramo: {inclinacionStr}\nInclinación total: {totalInclination:F1}°",
                        segmentCrew,
                        segmentMachine,
                        segment.Id
                    );

                    bool isExcavation = !string.IsNullOrEmpty(segment.WorkType) && 
                                      (segment.WorkType.IndexOf("Excavat", StringComparison.OrdinalIgnoreCase) >= 0 || 
                                       segment.WorkType.IndexOf("Trench", StringComparison.OrdinalIgnoreCase) >= 0);
                                       
                    if (isExcavation && (i == segment.Geometry.Count / 2)) 
                    {
                        double midLat = (pA.Latitude + pB.Latitude) / 2.0;
                        double midLon = (pA.Longitude + pB.Longitude) / 2.0;

                        GameObject shovelWrapper = new GameObject($"Shovel_{segment.Id}");
                        shovelWrapper.transform.SetParent(segmentsParent.transform, false);
                        var shovelAnchor = shovelWrapper.AddComponent<CesiumGlobeAnchor>();
                        shovelAnchor.longitudeLatitudeHeight = new Unity.Mathematics.double3(midLon, midLat, 30.0); 
                        
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
                            
                            var rotator = shovelWrapper.AddComponent<IconRotator>();

                            var gazeScaler = sVis.AddComponent<GazeScaler>();
                            gazeScaler.baseScale = new Vector3(300f, 300f, 300f); 
                            gazeScaler.hoverScale = new Vector3(1000f, 1000f, 1000f); 
                        }
                    }
                    
                    totalLines++;
                }

                FencingRenderer.RenderSegmentFencingAsync(segment, segmentsParent, repository);
                
                // --- NUEVO: Renderizar cuadrilla (si corresponde) ---
                CrewRenderer.RenderCrewOnSegment(segment, segmentsParent);
            }
            Debug.Log($"[Cesium] Segmentos renderizados con éxito: {totalLines} líneas creadas dinámicamente.");

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.transform.position = new Vector3(0, 150f, -150f); 
                mainCam.transform.LookAt(Vector3.zero);
            }
        }
    }
}
