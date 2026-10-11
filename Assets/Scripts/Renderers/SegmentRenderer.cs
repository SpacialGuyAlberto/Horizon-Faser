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
                    controlPoints.Add(tempObj.transform.localPosition); // Local Space position
                }
                UnityEngine.Object.Destroy(tempObj);

                // --- NUEVO: Interpolar spline matemáticamente en C# de forma Adaptativa ---
                // Si el dashboard mandó solo 3-4 puntos base, esto genera una curva perfecta.
                // Tolerancia de 0.05f metros (5 cm) asegura máxima calidad visual con mínimos vértices en RAM
                List<Vector3> smoothPoints = EvaluateCatmullRom(controlPoints, 0.05f);

                float totalInclination = 0f;
                float distance = 0f;
                for (int i = 0; i < smoothPoints.Count - 1; i++)
                {
                    distance += Vector3.Distance(smoothPoints[i], smoothPoints[i+1]);
                    if (i < smoothPoints.Count - 2)
                    {
                        Vector3 v1 = (smoothPoints[i+1] - smoothPoints[i]).normalized;
                        Vector3 v2 = (smoothPoints[i+2] - smoothPoints[i+1]).normalized;
                        totalInclination += Vector3.Angle(v1, v2);
                    }
                }

                // 2. Crear UN SOLO GameObject para TODO el Segmento
                GameObject segmentObj = new GameObject($"Segment_{segment.Id}");
                segmentObj.transform.SetParent(segmentsParent.transform, false);
                segmentObj.transform.localPosition = Vector3.zero;
                segmentObj.transform.localRotation = Quaternion.identity;

                // 3. Renderizar usando el LineRenderer pero con la TEXTURA 3D REALISTA del FBX nuevo!
                LineRenderer lineRenderer = segmentObj.AddComponent<LineRenderer>();
                lineRenderer.useWorldSpace = false; // smoothPoints están en Local Space de georeference
                lineRenderer.startWidth = 0.5f;
                lineRenderer.endWidth = 0.5f;
                lineRenderer.positionCount = smoothPoints.Count;
                lineRenderer.SetPositions(smoothPoints.ToArray());
                lineRenderer.alignment = LineAlignment.View;

                // Crear un material URP basado en las NUEVAS texturas del FBX para que tenga físicas de luz
                string basePath = "Models/FiberDuct/Meshy_AI_corrugated_microduct__1010224537_image-to-3d-texture";
                Material urpMat = PipeMaterialFactory.Get(basePath, segmentMaterial.color, 0.0f, 0.3f);

                if (urpMat != null)
                {
                    lineRenderer.material = urpMat;
                    lineRenderer.textureMode = LineTextureMode.Tile;
                }
                else
                {
                    Debug.LogWarning("[Cesium] Falló la carga de la nueva textura, usando fallback.");
                    lineRenderer.material = segmentMaterial;
                }

                // 4. Hitbox Ligero (Cajas separadas para evitar MeshColliders pesados)
                int colliderStep = Mathf.Max(1, smoothPoints.Count / 10); 
                for (int i = 0; i < smoothPoints.Count - 1; i += colliderStep)
                {
                    GameObject colObj = new GameObject("Hitbox");
                    colObj.transform.SetParent(segmentObj.transform, false);
                    colObj.transform.localPosition = (smoothPoints[i] + smoothPoints[i+1]) / 2f;
                    Vector3 lookDir = (smoothPoints[i+1] - smoothPoints[i]).normalized;
                    if (lookDir != Vector3.zero) colObj.transform.localRotation = Quaternion.LookRotation(lookDir);
                    
                    BoxCollider bc = colObj.AddComponent<BoxCollider>();
                    bc.size = new Vector3(1f, 1f, Vector3.Distance(smoothPoints[i], smoothPoints[i+1]));
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

        private static List<Vector3> EvaluateCatmullRom(List<Vector3> points, float tolerance = 0.05f)
        {
            if (points == null || points.Count < 2) return points ?? new List<Vector3>();
            if (points.Count == 2) return new List<Vector3> { points[0], points[1] };
            
            List<Vector3> result = new List<Vector3>();
            float toleranceSq = tolerance * tolerance;

            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector3 p0 = points[Mathf.Max(i - 1, 0)];
                Vector3 p1 = points[i];
                Vector3 p2 = points[i + 1];
                Vector3 p3 = points[Mathf.Min(i + 2, points.Count - 1)];

                SubdivideCatmullRom(p0, p1, p2, p3, 0f, 1f, result, toleranceSq);
            }
            // Add the very last point of the curve
            result.Add(points[points.Count - 1]);
            
            return result;
        }

        private static void SubdivideCatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t0, float t1, List<Vector3> result, float toleranceSq)
        {
            float tm = (t0 + t1) * 0.5f;
            Vector3 v0 = GetCatmullRomPosition(t0, p0, p1, p2, p3);
            Vector3 v1 = GetCatmullRomPosition(t1, p0, p1, p2, p3);
            Vector3 vm = GetCatmullRomPosition(tm, p0, p1, p2, p3);
            
            // Calculamos la distancia del punto central a la línea recta entre los extremos
            float distSq = SqrDistanceToSegment(vm, v0, v1);
            
            // Si el punto central se desvía más de la tolerancia de la línea recta, subdividimos
            // Limitamos a un máximo de 20 subdivisiones (0.05f) para evitar recursión infinita en curvas extremas
            if (distSq > toleranceSq && (t1 - t0) > 0.05f) 
            {
                SubdivideCatmullRom(p0, p1, p2, p3, t0, tm, result, toleranceSq);
                SubdivideCatmullRom(p0, p1, p2, p3, tm, t1, result, toleranceSq);
            }
            else
            {
                // Solo guardamos el punto inicial del segmento, el punto final será guardado por la siguiente llamada
                result.Add(v0);
            }
        }

        private static float SqrDistanceToSegment(Vector3 point, Vector3 segA, Vector3 segB)
        {
            Vector3 lineDir = segB - segA;
            float lineLenSq = lineDir.sqrMagnitude;
            if (lineLenSq == 0f) return (point - segA).sqrMagnitude;
            
            float t = Mathf.Clamp01(Vector3.Dot(point - segA, lineDir) / lineLenSq);
            Vector3 projection = segA + t * lineDir;
            return (point - projection).sqrMagnitude;
        }

        private static Vector3 GetCatmullRomPosition(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            Vector3 a = 2f * p1;
            Vector3 b = p2 - p0;
            Vector3 c = 2f * p0 - 5f * p1 + 4f * p2 - p3;
            Vector3 d = -p0 + 3f * p1 - 3f * p2 + p3;

            return 0.5f * (a + (b * t) + (c * t * t) + (d * t * t * t));
        }
    }
}
