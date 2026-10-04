using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using HorizonFaser.Models;

namespace HorizonFaser.UI
{
    public class DashboardUIController : MonoBehaviour
    {
        [Header("UI References")]
        public TextMeshProUGUI welcomeText;
        public TextMeshProUGUI statsText;
        public Button openCesiumButton;
        public Button logoutButton;

        private Repositories.IFiberRepository _repository;
        private System.Collections.Generic.List<Models.QuarkbauProject> _projects;
        private System.Collections.Generic.List<Models.Cluster> _currentClusters;
        private System.Collections.Generic.List<Models.Pop> _currentPops;

        private Models.QuarkbauProject _selectedProject;
        private Models.Cluster _selectedCluster;
        private Models.Pop _selectedPop;

        private GameObject _listContainer;

        private void Awake()
        {
            // Ocultar UI vieja
            if (openCesiumButton != null) openCesiumButton.gameObject.SetActive(false);
            if (logoutButton != null) logoutButton.gameObject.SetActive(false);
            if (statsText != null) statsText.gameObject.SetActive(false);

            if (welcomeText != null)
            {
                welcomeText.fontSize = 42;
                welcomeText.color = Color.white;
                welcomeText.raycastTarget = false; // PREVENT IT FROM BLOCKING CLICKS
            }

            // Evitar que el panel de fondo bloquee los clics
            Transform bgPanel = transform.Find("BackgroundPanel");
            if (bgPanel != null)
            {
                var bgImage = bgPanel.GetComponent<Image>();
                if (bgImage != null) bgImage.raycastTarget = false;
            }

            if (_listContainer == null) CreateListContainer();

            // Destruir ABSOLUTAMENTE TODOS los raycasters previos (incluido el de XR que bloquea el ratón)
            var raycasters = GetComponents<UnityEngine.UI.GraphicRaycaster>();
            foreach(var r in raycasters)
            {
                DestroyImmediate(r);
            }

            // Añadir un GraphicRaycaster limpio y puro
            var raycaster = gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            raycaster.ignoreReversedGraphics = false; // Permitir clics desde cualquier ángulo

            var cg = GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }

            // ASIGNAR EVENT CAMERA EN AWAKE (Antes de que GraphicRaycaster se inicialice)
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                Canvas canvas = GetComponent<Canvas>();
                if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
                {
                    canvas.worldCamera = mainCam;
                }
            }
        }

        private void Start()
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                // Copiar EXACTAMENTE la lógica de posicionamiento del LoginUIController que SÍ funciona
                transform.position = mainCam.transform.position + mainCam.transform.forward * 1.5f;
                transform.rotation = Quaternion.LookRotation(transform.position - mainCam.transform.position);
            }
            
            // Iniciar corrutina para forzar la longitud del láser XR continuamente
            StartCoroutine(ForceXRLaserDistanceRoutine());

            // Crear el Imán de Puntero (Magnet) para selección sin físicas
            if (FindObjectOfType<HorizonFaser.XR.XRPointerMagnet>() == null)
            {
                GameObject magnet = new GameObject("XRPointerMagnet");
                magnet.AddComponent<HorizonFaser.XR.XRPointerMagnet>();
            }
        }

        private System.Collections.IEnumerator ForceXRLaserDistanceRoutine()
        {
            while (true)
            {
                // Aumentamos el alcance físico del raycast a 50km
                var rayInteractors = UnityEngine.Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
                foreach (var ray in rayInteractors)
                {
                    if (ray.maxRaycastDistance < 50000f) ray.maxRaycastDistance = 50000f;
                }

                // Aumentamos el largo VISUAL del láser (la línea roja/blanca)
                var lineVisuals = UnityEngine.Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals.XRInteractorLineVisual>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
                foreach (var line in lineVisuals)
                {
                    if (line.lineLength < 50000f) line.lineLength = 50000f;
                }

                yield return new WaitForSeconds(1.5f);
            }
        }

        private void CreateListContainer()
        {
            _listContainer = new GameObject("WizardListContainer");
            _listContainer.transform.SetParent(this.transform, false);
            _listContainer.transform.SetAsLastSibling(); // Asegurar que esté por encima de todos los demás elementos UI
            
            var rt = _listContainer.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, -60);
            rt.sizeDelta = new Vector2(500, 400);

            var vlg = _listContainer.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            vlg.spacing = 15;
            vlg.childControlHeight = false;
            vlg.childControlWidth = false;
            vlg.childAlignment = TextAnchor.UpperCenter;
        }

        public void Setup(Repositories.IFiberRepository repository, System.Collections.Generic.List<Models.QuarkbauProject> initialProjects)
        {
            if (_listContainer == null) CreateListContainer();
            
            _repository = repository;
            _projects = initialProjects;

            // AUTO SELECT PROJECT 6 Y RENDERIZAR DIRECTO CON CESIUM
            var p6 = _projects.Find(p => p.Id == 6);
            if (p6 != null)
            {
                AutoLoadAndRenderProject(p6);
            }
            else
            {
                ShowProjects();
            }
        }

        private async void AutoLoadAndRenderProject(Models.QuarkbauProject project)
        {
            if (welcomeText != null) welcomeText.text = $"Cargando Segmentos del Proyecto {project.Id}...";
            ClearList();
            
            var segments = await _repository.FetchProjectSegmentsByIdAsync(project.Id);
            if (segments == null || segments.Count == 0)
            {
                if (welcomeText != null) welcomeText.text = "No hay segmentos.";
                return;
            }

            if (welcomeText != null) welcomeText.text = $"Renderizando {segments.Count} segmentos en Cesium...";
            
            RenderSegmentsCesium(segments);

            try
            {
                var clusters = await _repository.FetchClusterByProjectIdAsync(project.Id);
                RenderPopsAndNvtsCesium(clusters);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Cesium] No se pudieron cargar los clusters/POPs: {ex.Message}");
            }

            try
            {
                var utilities = await _repository.FetchExternalUtilitiesAsync();
                if (utilities != null && utilities.Count > 0)
                {
                    RenderUtilitiesCesium(utilities);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Cesium] No se pudieron cargar las utilidades externas: {ex.Message}");
            }
        }

        private async void RenderSegmentsCesium(System.Collections.Generic.List<Models.QuarkBauSegment> segments)
        {
            // Ocultar el UI del Dashboard
            var cg = GetComponent<CanvasGroup>();
            if (cg != null) { cg.alpha = 0; cg.interactable = false; cg.blocksRaycasts = false; }
            
            var georeference = FindObjectOfType<CesiumForUnity.CesiumGeoreference>();
            if (georeference == null)
            {
                Debug.LogWarning("[Cesium] No se encontró CesiumGeoreference en la escena. Creando uno dinámicamente...");
                GameObject geoObj = new GameObject("CesiumGeoreference");
                georeference = geoObj.AddComponent<CesiumForUnity.CesiumGeoreference>();
            }

            // Calcular el centro exacto del proyecto (Bounding Box)
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

            if (hasValidCoords)
            {
                georeference.longitude = (minLng + maxLng) / 2.0;
                georeference.latitude = (minLat + maxLat) / 2.0;
                georeference.height = 0;
            }
            else
            {
                georeference.longitude = 10.0;
                georeference.latitude = 51.0;
                georeference.height = 0;
            }

            // ESPERAR 1 FRAME PARA QUE CESIUM INICIALICE SUS MATRICES INTERNAS (Evita que spawneen a millones de km)
            await System.Threading.Tasks.Task.Yield();

            GameObject segmentsParent = new GameObject("Cesium_Segments_Project6");
            segmentsParent.transform.SetParent(georeference.transform, false);

            // Usar UNLIT COLOR para que brille intenso sin importar si hay luces en la escena
            Material segmentMaterial = new Material(Shader.Find("Unlit/Color"));
            segmentMaterial.color = new Color(1f, 0.5f, 0f, 1f); // Naranja brillante como pediste

            int totalLines = 0;
            foreach (var segment in segments)
            {
                if (segment.Geometry == null || segment.Geometry.Count < 2) continue;

                // PRE-CALCULAR INCLINACIONES (Ángulo entre vectores consecutivos)
                List<Vector3> tempPositions = new List<Vector3>();
                foreach (var p in segment.Geometry)
                {
                    GameObject tempAnchor = new GameObject("TempAnchor");
                    tempAnchor.transform.SetParent(segmentsParent.transform, false);
                    var globeAnchor = tempAnchor.AddComponent<CesiumForUnity.CesiumGlobeAnchor>();
                    globeAnchor.longitudeLatitudeHeight = new Unity.Mathematics.double3(p.Longitude, p.Latitude, 0);
                    tempPositions.Add(tempAnchor.transform.position);
                    Destroy(tempAnchor); // Destruir tras leer la posición local
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

                    // 1. Crear un ancla para el Punto A
                    GameObject anchorObjA = new GameObject($"Seg_{segment.Id}_A_{i}");
                    anchorObjA.transform.SetParent(segmentsParent.transform, false);
                    var globeAnchorA = anchorObjA.AddComponent<CesiumForUnity.CesiumGlobeAnchor>();
                    globeAnchorA.longitudeLatitudeHeight = new Unity.Mathematics.double3(pA.Longitude, pA.Latitude, 0);

                    // 2. Crear un ancla para el Punto B
                    GameObject anchorObjB = new GameObject($"Seg_{segment.Id}_B_{i}");
                    anchorObjB.transform.SetParent(segmentsParent.transform, false);
                    var globeAnchorB = anchorObjB.AddComponent<CesiumForUnity.CesiumGlobeAnchor>();
                    globeAnchorB.longitudeLatitudeHeight = new Unity.Mathematics.double3(pB.Longitude, pB.Latitude, 0);

                    // ESPERAR A QUE CESIUM ACTUALICE LAS POSICIONES LOCALES
                    // Las coordenadas se sincronizan en el engine, forzamos un refesh si es posible o leemos directo
                    Vector3 posA = anchorObjA.transform.position;
                    Vector3 posB = anchorObjB.transform.position;

                    Vector3 centerPos = (posA + posB) / 2f;
                    float distance = Vector3.Distance(posA, posB);
                    Vector3 direction = (posB - posA).normalized;

                    // 3. Crear el contenedor lógico del segmento (lo que maneja la escala y rotación)
                    GameObject cyl = new GameObject($"Segment_{segment.Id}_Line_{i}");
                    cyl.transform.SetParent(segmentsParent.transform, true);

                    // 4. Instanciar el FBX del Tubo Corrugado como HIJO del contenedor
                    GameObject ductPrefab = Resources.Load<GameObject>("Models/Meshy_AI_corrugated_microduct/Meshy_AI_corrugated_microduct__1001094510_image-to-3d-texture");
                    GameObject ductVisual;
                    
                    if (ductPrefab != null)
                    {
                        ductVisual = UnityEngine.Object.Instantiate(ductPrefab);
                        ductVisual.transform.SetParent(cyl.transform, false);
                        
                        // AUTO-ALINEACIÓN Y ESCALA MÁGICA
                        // Buscamos la malla del FBX para saber cómo viene orientado de fábrica
                        MeshFilter mf = ductVisual.GetComponentInChildren<MeshFilter>();
                        if (mf != null && mf.sharedMesh != null)
                        {
                            Vector3 boundsSize = mf.sharedMesh.bounds.size;
                            
                            // Determinar cuál es el eje más largo (la longitud real del tubo)
                            float maxAxis = Mathf.Max(boundsSize.x, boundsSize.y, boundsSize.z);
                            
                            // Rotarlo para que su eje más largo apunte hacia ARRIBA (Eje Y local del padre)
                            if (maxAxis == boundsSize.x)
                            {
                                ductVisual.transform.localRotation = Quaternion.Euler(0, 0, 90); // X a Y
                                // Ajustar escala para que mida exactamente 2 unidades de largo (como un Cilindro nativo)
                                float scaleModifier = 2f / boundsSize.x;
                                ductVisual.transform.localScale = new Vector3(scaleModifier, scaleModifier, scaleModifier);
                            }
                            else if (maxAxis == boundsSize.z)
                            {
                                ductVisual.transform.localRotation = Quaternion.Euler(90, 0, 0); // Z a Y
                                float scaleModifier = 2f / boundsSize.z;
                                ductVisual.transform.localScale = new Vector3(scaleModifier, scaleModifier, scaleModifier);
                            }
                            else
                            {
                                // Ya está orientado en Y
                                float scaleModifier = 2f / boundsSize.y;
                                ductVisual.transform.localScale = new Vector3(scaleModifier, scaleModifier, scaleModifier);
                            }
                        }

                        // Aplicar Material URP PBR para realismo
                        string basePath = "Models/Meshy_AI_corrugated_microduct/Meshy_AI_corrugated_microduct__1001094510_image-to-3d-texture";
                        Texture2D tex = Resources.Load<Texture2D>(basePath);
                        Texture2D normalTex = Resources.Load<Texture2D>(basePath + "_normal");

                        Material urpMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                        
                        // Textura base (Color)
                        if (tex != null) urpMat.mainTexture = tex;
                        else urpMat.color = segmentMaterial.color;

                        // Mapa de normales (Relieve/Rugosidad)
                        if (normalTex != null)
                        {
                            urpMat.SetTexture("_BumpMap", normalTex);
                            urpMat.EnableKeyword("_NORMALMAP");
                        }
                        
                        // Ajustes de plástico para el tubo (no es metálico, pero sí tiene brillo)
                        urpMat.SetFloat("_Metallic", 0.0f);
                        urpMat.SetFloat("_Smoothness", 0.6f); // Brillo tipo plástico

                        foreach (var r in ductVisual.GetComponentsInChildren<Renderer>())
                        {
                            r.material = urpMat;
                        }
                    }
                    else
                    {
                        // Fallback si no carga el FBX
                        ductVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        ductVisual.transform.SetParent(cyl.transform, false);
                        ductVisual.GetComponent<MeshRenderer>().material = segmentMaterial;
                    }

                    // Necesitamos que el objeto padre tenga un BoxCollider para que funcione el clic
                    BoxCollider box = cyl.AddComponent<BoxCollider>();
                    // Tamaño 2 de alto en Y (porque SegmentConnector asume cilindro primitivo)
                    box.size = new Vector3(1f, 2f, 1f); 

                    
                    // Añadir script de conexión dinámica para que se actualice CADA FRAME
                    var connector = cyl.AddComponent<SegmentConnector>();
                    connector.PointA = anchorObjA.transform;
                    connector.PointB = anchorObjB.transform;
                    connector.Thickness = 0.5f; // Medio metro de grosor para que se vea bien desde lejos
                    
                    // --- NUESTRO SCRIPT DE INTERACCIÓN ---
                    string inclinacionStr = (i == 0) ? "N/A (Inicio)" : $"{currentInclination:F1}°";
                    HorizonFaser.XR.NetworkNodeInteractable.AttachToNode(
                        cyl, 
                        HorizonFaser.XR.NetworkNodeType.Segmento, 
                        $"Segmento {segment.Id}", 
                        $"Longitud: {distance:F1}m\nInclinación tramo: {inclinacionStr}\nInclinación total: {totalInclination:F1}°"
                    );

                    // --- AÑADIR ICONO DE EXCAVACIÓN / PALA ---
                    bool isExcavation = !string.IsNullOrEmpty(segment.WorkType) && 
                                      (segment.WorkType.IndexOf("Excavat", StringComparison.OrdinalIgnoreCase) >= 0 || 
                                       segment.WorkType.IndexOf("Trench", StringComparison.OrdinalIgnoreCase) >= 0);
                                       
                    // Solo ponemos 1 pala en el medio de TODO el segmento para no saturar visualmente
                    if (isExcavation && (i == segment.Geometry.Count / 2)) 
                    {
                        double midLat = (pA.Latitude + pB.Latitude) / 2.0;
                        double midLon = (pA.Longitude + pB.Longitude) / 2.0;

                        GameObject shovelWrapper = new GameObject($"Shovel_{segment.Id}");
                        shovelWrapper.transform.SetParent(segmentsParent.transform, false);
                        var shovelAnchor = shovelWrapper.AddComponent<CesiumForUnity.CesiumGlobeAnchor>();
                        // Lo bajamos a 30 metros para que no quede fuera de la vista de la cámara
                        shovelAnchor.longitudeLatitudeHeight = new Unity.Mathematics.double3(midLon, midLat, 30.0); 
                        
                        GameObject shovelPrefab = Resources.Load<GameObject>("Models/ShovelIcon/Meshy_AI_macos_shovel_dirt_ico_1002155123_image-to-3d-texture");
                        if (shovelPrefab != null)
                        {
                            GameObject sVis = UnityEngine.Object.Instantiate(shovelPrefab);
                            sVis.transform.SetParent(shovelWrapper.transform, false);
                            // Escala ajustada a un tamaño muy grande pero visible (evitando culling de la cámara)
                            sVis.transform.localScale = new Vector3(300f, 300f, 300f); 
                            
                            // Restaurar la textura original pero activar Alpha Clipping (Cutout)
                            // Esto eliminará el cubo transparente/mosaico que rodeaba a la pala
                            string shovelPath = "Models/ShovelIcon/Meshy_AI_macos_shovel_dirt_ico_1002155123_image-to-3d-texture";
                            Texture2D sTex = Resources.Load<Texture2D>(shovelPath);

                            Material sMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                            if (sTex != null) sMat.mainTexture = sTex;
                            
                            // Activar Alpha Test para que los bordes del bloque desaparezcan
                            sMat.SetFloat("_AlphaClip", 1.0f);
                            sMat.EnableKeyword("_ALPHATEST_ON");
                            // IMPORTANTE: Un valor muy bajo (0.05f) evita que la pala entera desaparezca de lejos por el MipMapping
                            sMat.SetFloat("_Cutoff", 0.05f); 
                            
                            sMat.SetFloat("_Metallic", 0.0f);
                            sMat.SetFloat("_Smoothness", 0.1f);
                            
                            foreach (var r in sVis.GetComponentsInChildren<Renderer>())
                            {
                                r.material = sMat;
                            }
                            
                            // Hacer que la pala rote lentamente para llamar la atención
                            var rotator = shovelWrapper.AddComponent<IconRotator>();

                            // Añadir escalador dinámico basado en la mirada (Gaze)
                            var gazeScaler = sVis.AddComponent<GazeScaler>();
                            gazeScaler.baseScale = new Vector3(300f, 300f, 300f); 
                            gazeScaler.hoverScale = new Vector3(1000f, 1000f, 1000f); 
                        }
                    }
                    
                    totalLines++;
                }
            }
            Debug.Log($"[Cesium] Segmentos renderizados con éxito: {totalLines} líneas creadas dinámicamente.");

            // Mover la cámara para tener una buena vista de pájaro de los segmentos
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.transform.position = new Vector3(0, 150f, -150f); 
                mainCam.transform.LookAt(Vector3.zero);
            }

            // Ocultar el Canvas en lugar de destruirlo para que los métodos async (Pops y NVTs) no exploten.
            // gameObject.SetActive(false); // Eliminado para no romper RenderUtilitiesCesium que viene después.
        }

        private void RenderPopsAndNvtsCesium(List<Cluster> clusters)
        {
            // El fix de distancia del láser XR ahora se maneja en ForceXRLaserDistanceRoutine() continuamente.

            var georeference = FindObjectOfType<CesiumForUnity.CesiumGeoreference>();
            if (georeference == null) return;

            GameObject nodesParent = new GameObject("Cesium_Nodes_Project6");
            nodesParent.transform.SetParent(georeference.transform, false);

            Material popMaterial = new Material(Shader.Find("Unlit/Color"));
            popMaterial.color = new Color(1f, 0.4f, 0f, 1f); // Naranja para POPs

            Material nvtMaterial = new Material(Shader.Find("Unlit/Color"));
            nvtMaterial.color = new Color(1f, 1f, 0f, 1f); // Amarillo para NVTs

            int popCount = 0;
            int nvtCount = 0;

            foreach (var cluster in clusters)
            {
                if (cluster.Pops == null) continue;

                foreach (var pop in cluster.Pops)
                {
                    GameObject popPrefab = Resources.Load<GameObject>("Models/POP/Meshy_AI_pop_shelter_3d_1001095043_image-to-3d-texture");
                    GameObject popObj;
                    
                    if (popPrefab != null)
                    {
                        popObj = UnityEngine.Object.Instantiate(popPrefab);
                        popObj.name = $"POP_{pop.Id}";
                        popObj.transform.SetParent(nodesParent.transform, false);
                        
                        // Escala: Ajustar a gusto, probamos con 30f como base.
                        popObj.transform.localScale = new Vector3(30f, 30f, 30f); 

                        // Material URP dinámico con Normal Map
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
                        popMat.SetFloat("_Smoothness", 0.3f); // Hormigón/Metal con poco brillo

                        foreach (var renderer in popObj.GetComponentsInChildren<Renderer>())
                        {
                            renderer.material = popMat;
                        }
                    }
                    else
                    {
                        // Fallback
                        popObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        popObj.name = $"POP_{pop.Id}_Fallback";
                        popObj.transform.SetParent(nodesParent.transform, false);
                        popObj.transform.localScale = new Vector3(8f, 8f, 8f);
                        popObj.GetComponent<MeshRenderer>().material = popMaterial;
                    }

                    // Añadir script de interacción
                    string popId = pop.Id.ToString();
                    string popName = pop.Name;
                    HorizonFaser.XR.NetworkNodeInteractable.AttachToNode(
                        popObj, 
                        HorizonFaser.XR.NetworkNodeType.Pop, 
                        $"POP {popName}", 
                        $"ID: {popId}\nPos: {pop.Latitude:F5}, {pop.Longitude:F5}"
                    );

                    // Anclar a Cesium (Altura 0 para el suelo)
                    var popAnchor = popObj.AddComponent<CesiumForUnity.CesiumGlobeAnchor>();
                    popAnchor.longitudeLatitudeHeight = new Unity.Mathematics.double3(pop.Longitude, pop.Latitude, 0);
                    popCount++;

                    if (pop.Nvts == null) continue;

                    foreach (var nvt in pop.Nvts)
                    {
                        GameObject nvtPrefab = Resources.Load<GameObject>("Models/Meshy_Ai_street_cabinet/Meshy_AI_street_cabinet_3d_1001093310_image-to-3d-texture");
                        GameObject nvtObj;
                        
                        if (nvtPrefab != null)
                        {
                            nvtObj = UnityEngine.Object.Instantiate(nvtPrefab);
                            nvtObj.name = $"NVT_{nvt.Id}";
                            nvtObj.transform.SetParent(nodesParent.transform, false);
                            
                            // 1. Escala: Los FBX a veces vienen en centímetros (0.01), así que lo escalamos más grande (100)
                            // Si lo ves muy grande, bájalo a 10f o 3f.
                            nvtObj.transform.localScale = new Vector3(30f, 30f, 30f); 

                            // Material URP dinámico
                            string nvtBasePath = "Models/Meshy_Ai_street_cabinet/Meshy_AI_street_cabinet_3d_1001093310_image-to-3d-texture";
                            Texture2D nvtTex = Resources.Load<Texture2D>(nvtBasePath);
                            Texture2D nvtNormalTex = Resources.Load<Texture2D>(nvtBasePath + "_normal");

                            Material urpMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                            if (nvtTex != null) urpMat.mainTexture = nvtTex;
                            else urpMat.color = Color.yellow; // Fallback visual

                            if (nvtNormalTex != null)
                            {
                                urpMat.SetTexture("_BumpMap", nvtNormalTex);
                                urpMat.EnableKeyword("_NORMALMAP");
                            }
                            
                            urpMat.SetFloat("_Metallic", 0.6f); // El NVT suele ser de metal
                            urpMat.SetFloat("_Smoothness", 0.4f);

                            // Aplicarlo a todas las mallas del FBX
                            foreach (var renderer in nvtObj.GetComponentsInChildren<Renderer>())
                            {
                                renderer.material = urpMat;
                            }
                        }
                        else
                        {
                            // Fallback por si Unity aún no ha importado el FBX
                            nvtObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            nvtObj.name = $"NVT_{nvt.Id}_Fallback";
                            nvtObj.transform.SetParent(nodesParent.transform, false);
                            nvtObj.transform.localScale = new Vector3(3f, 6f, 2f);
                            nvtObj.GetComponent<MeshRenderer>().material = nvtMaterial;
                        }

                        // Añadir la interacción 3D para que salga la Card al clickear/apuntar
                        string nvtId = nvt.Id.ToString();
                        string nvtNameFixed = nvt.Identifier; 
                        HorizonFaser.XR.NetworkNodeInteractable.AttachToNode(
                            nvtObj, 
                            HorizonFaser.XR.NetworkNodeType.Netzverteiler, 
                            $"NVT {nvtNameFixed}", 
                            $"ID: {nvtId}\nPos: {nvt.Latitude:F5}, {nvt.Longitude:F5}"
                        );

                        // Anclarlo geográficamente con Cesium
                        var nvtAnchor = nvtObj.AddComponent<CesiumForUnity.CesiumGlobeAnchor>();
                        nvtAnchor.longitudeLatitudeHeight = new Unity.Mathematics.double3(nvt.Longitude, nvt.Latitude, 0); // Altura 0 para que repose en el suelo
                        nvtCount++;
                    }
                }
            }

            Debug.Log($"[Cesium] Nodos: {popCount} POPs (naranja 10m) y {nvtCount} NVTs (amarillo 3m).");
        }

        private async void RenderUtilitiesCesium(System.Collections.Generic.List<Models.UtilityLine> utilities)
        {
            var georeference = FindObjectOfType<CesiumForUnity.CesiumGeoreference>();
            if (georeference == null) return;

            // Wait 1 frame to ensure coordinate initialization
            await System.Threading.Tasks.Task.Yield();

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
                    Destroy(tempAnchor);
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

                        Texture2D tex = Resources.Load<Texture2D>(basePath);
                        Texture2D normalTex = Resources.Load<Texture2D>(basePath + "_normal");
                        Texture2D metallicTex = Resources.Load<Texture2D>(basePath + "_metallic");

                        Material urpMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                        
                        if (tex != null) urpMat.mainTexture = tex;
                        else urpMat.color = fallbackColor;

                        if (normalTex != null)
                        {
                            urpMat.SetTexture("_BumpMap", normalTex);
                            urpMat.EnableKeyword("_NORMALMAP");
                        }
                        
                        if (metallicTex != null)
                        {
                            urpMat.SetTexture("_MetallicGlossMap", metallicTex);
                            urpMat.EnableKeyword("_METALLICGLOSSMAP");
                        }
                        
                        urpMat.SetFloat("_Metallic", 0.3f); // Water pipe visual tweak
                        urpMat.SetFloat("_Smoothness", 0.8f);

                        foreach (var r in utilVisual.GetComponentsInChildren<Renderer>())
                        {
                            r.material = urpMat;
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

        private GameObject _activeNodeCard;

        public void ShowNodeCard(string type, string id, string name, double lat, double lon)
        {
            if (_activeNodeCard != null) Destroy(_activeNodeCard);

            Camera mainCam = Camera.main;
            if (mainCam == null) return;

            // 1. Crear el Canvas flotante (Card)
            _activeNodeCard = new GameObject("NodeInfoCard");
            // Lo ponemos flotando frente al usuario (1.5 metros adelante)
            _activeNodeCard.transform.position = mainCam.transform.position + mainCam.transform.forward * 1.5f;
            _activeNodeCard.transform.LookAt(_activeNodeCard.transform.position + mainCam.transform.forward);
            _activeNodeCard.transform.localScale = new Vector3(0.002f, 0.002f, 0.002f);

            Canvas canvas = _activeNodeCard.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            _activeNodeCard.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            RectTransform canvasRT = _activeNodeCard.GetComponent<RectTransform>();
            canvasRT.sizeDelta = new Vector2(400, 250);

            // 2. Fondo estilo Meta Horizon OS (Oscuro, translúcido, bordes suaves simulados)
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(_activeNodeCard.transform, false);
            UnityEngine.UI.Image bgImage = bgObj.AddComponent<UnityEngine.UI.Image>();
            bgImage.color = new Color(0.12f, 0.13f, 0.16f, 0.9f); // Dark Slate translúcido
            RectTransform bgRT = bgObj.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = Vector2.zero;

            // 3. Barra decorativa superior (Color según tipo)
            GameObject headerBarObj = new GameObject("HeaderBar");
            headerBarObj.transform.SetParent(bgObj.transform, false);
            UnityEngine.UI.Image headerImg = headerBarObj.AddComponent<UnityEngine.UI.Image>();
            headerImg.color = (type == "POP") ? new Color(1f, 0.5f, 0.1f) : new Color(1f, 0.8f, 0.2f);
            RectTransform headerRT = headerBarObj.GetComponent<RectTransform>();
            headerRT.anchorMin = new Vector2(0, 1);
            headerRT.anchorMax = new Vector2(1, 1);
            headerRT.pivot = new Vector2(0.5f, 1);
            headerRT.sizeDelta = new Vector2(0, 10);
            headerRT.anchoredPosition = Vector2.zero;

            // 4. Texto del Título
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(bgObj.transform, false);
            var titleText = titleObj.AddComponent<UnityEngine.UI.Text>();
            titleText.text = $"{type} | {id}";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 28;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.UpperLeft;
            RectTransform titleRT = titleObj.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0, 1);
            titleRT.anchorMax = new Vector2(1, 1);
            titleRT.pivot = new Vector2(0, 1);
            titleRT.sizeDelta = new Vector2(360, 40);
            titleRT.anchoredPosition = new Vector2(20, -30);

            // 5. Texto de Información (Nombre)
            GameObject nameObj = new GameObject("NameText");
            nameObj.transform.SetParent(bgObj.transform, false);
            var nameText = nameObj.AddComponent<UnityEngine.UI.Text>();
            nameText.text = $"Nombre: {(!string.IsNullOrEmpty(name) ? name : "Sin Nombre")}";
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = 20;
            nameText.color = new Color(0.8f, 0.8f, 0.85f); // Gris claro
            nameText.alignment = TextAnchor.UpperLeft;
            RectTransform nameRT = nameObj.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0, 1);
            nameRT.anchorMax = new Vector2(1, 1);
            nameRT.pivot = new Vector2(0, 1);
            nameRT.sizeDelta = new Vector2(360, 30);
            nameRT.anchoredPosition = new Vector2(20, -80);

            // 6. Texto de Coordenadas
            GameObject coordObj = new GameObject("CoordText");
            coordObj.transform.SetParent(bgObj.transform, false);
            var coordText = coordObj.AddComponent<UnityEngine.UI.Text>();
            coordText.text = $"Lat: {lat:F6}\nLon: {lon:F6}";
            coordText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            coordText.fontSize = 18;
            coordText.color = new Color(0.6f, 0.65f, 0.7f); // Gris más oscuro
            coordText.alignment = TextAnchor.UpperLeft;
            RectTransform coordRT = coordObj.GetComponent<RectTransform>();
            coordRT.anchorMin = new Vector2(0, 1);
            coordRT.anchorMax = new Vector2(1, 1);
            coordRT.pivot = new Vector2(0, 1);
            coordRT.sizeDelta = new Vector2(360, 50);
            coordRT.anchoredPosition = new Vector2(20, -120);

            // 7. Botón de Cerrar (Estilo limpio)
            GameObject closeBtnObj = new GameObject("CloseButton");
            closeBtnObj.transform.SetParent(bgObj.transform, false);
            UnityEngine.UI.Image closeBg = closeBtnObj.AddComponent<UnityEngine.UI.Image>();
            closeBg.color = new Color(0.2f, 0.22f, 0.25f, 1f);
            UnityEngine.UI.Button closeBtn = closeBtnObj.AddComponent<UnityEngine.UI.Button>();
            
            // XR INTERACTION: Para que el botón de cerrar funcione con el mando de Quest
            closeBtnObj.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();
            
            closeBtn.onClick.AddListener(() => Destroy(_activeNodeCard));

            RectTransform closeBtnRT = closeBtnObj.GetComponent<RectTransform>();
            closeBtnRT.anchorMin = new Vector2(0.5f, 0);
            closeBtnRT.anchorMax = new Vector2(0.5f, 0);
            closeBtnRT.pivot = new Vector2(0.5f, 0);
            closeBtnRT.sizeDelta = new Vector2(120, 40);
            closeBtnRT.anchoredPosition = new Vector2(0, 20);

            GameObject closeTextObj = new GameObject("CloseText");
            closeTextObj.transform.SetParent(closeBtnObj.transform, false);
            var closeText = closeTextObj.AddComponent<UnityEngine.UI.Text>();
            closeText.text = "Cerrar";
            closeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            closeText.fontSize = 18;
            closeText.color = Color.white;
            closeText.alignment = TextAnchor.MiddleCenter;
            RectTransform closeTextRT = closeTextObj.GetComponent<RectTransform>();
            closeTextRT.anchorMin = Vector2.zero;
            closeTextRT.anchorMax = Vector2.one;
            closeTextRT.sizeDelta = Vector2.zero;
        }

        private void ClearList()

        {
            if (_listContainer == null) return;
            foreach (Transform child in _listContainer.transform)
            {
                Destroy(child.gameObject);
            }
        }

        private Button CreateWizardButton(string text, UnityEngine.Events.UnityAction onClick)
        {
            // CREAR BOTÓN CON EL SISTEMA NATIVO DE UNITY (100% Garantizado que funciona el click)
            var resources = new UnityEngine.UI.DefaultControls.Resources();
            GameObject btnObj = UnityEngine.UI.DefaultControls.CreateButton(resources);
            btnObj.name = "Btn_" + text;
            btnObj.transform.SetParent(_listContainer.transform, false);
            
            var rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(450, 60);
            rt.localPosition = new Vector3(rt.localPosition.x, rt.localPosition.y, 0f);
            rt.localScale = Vector3.one;

            var img = btnObj.GetComponent<Image>();
            img.color = new Color32(50, 50, 50, 255);

            var btn = btnObj.GetComponent<Button>();
            
            // Hover colors (Meta style)
            ColorBlock cb = btn.colors;
            cb.normalColor = new Color32(50, 50, 50, 255);
            cb.highlightedColor = new Color32(0, 120, 255, 255); // Meta Blue
            cb.pressedColor = new Color32(0, 90, 200, 255);
            cb.selectedColor = new Color32(50, 50, 50, 255);
            btn.colors = cb;
            
            btn.onClick.AddListener(() => {
                Debug.Log($"[Dashboard] Click detectado en botón: {text}");
                if (onClick != null) onClick();
            });

            // AÑADIR BOX COLLIDER PARA CLICK POR FÍSICAS (Fallback)
            var col = btnObj.AddComponent<BoxCollider>();
            col.size = new Vector3(rt.sizeDelta.x, rt.sizeDelta.y, 1f);

            // Arreglar el texto por defecto que viene con DefaultControls (usa Text antiguo, no TextMeshPro)
            var oldText = btnObj.GetComponentInChildren<UnityEngine.UI.Text>();
            if (oldText != null) DestroyImmediate(oldText.gameObject);

            // Añadir TextMeshProUGUI
            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            var txtRt = txtObj.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;
            txtRt.localPosition = Vector3.zero;
            txtRt.localScale = Vector3.one;

            var tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.color = Color.white;
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false; 

            return btn;
        }

        private void ShowProjects()
        {
            if (welcomeText != null) welcomeText.text = "Selecciona un Proyecto";
            ClearList();

            if (_projects == null || _projects.Count == 0)
            {
                CreateWizardButton("No hay proyectos", null);
                return;
            }

            foreach (var p in _projects)
            {
                var capturedProject = p; // Capture variable for closure
                CreateWizardButton(p.Name, () => OnProjectSelected(capturedProject));
            }
        }

        private async void OnProjectSelected(Models.QuarkbauProject project)
        {
            _selectedProject = project;
            if (welcomeText != null) welcomeText.text = "Cargando Clusters...";
            ClearList();

            try
            {
                _currentClusters = await _repository.FetchClusterByProjectIdAsync(project.Id);
                
                if (welcomeText != null) welcomeText.text = $"Proyecto: {project.Name}\nSelecciona un Cluster";
                
                if (_currentClusters == null || _currentClusters.Count == 0)
                {
                    CreateWizardButton("Sin Clusters (Volver)", ShowProjects);
                    return;
                }

                CreateWizardButton("← Volver a Proyectos", ShowProjects).GetComponent<Image>().color = new Color32(100, 30, 30, 255);

                foreach (var c in _currentClusters)
                {
                    var capturedCluster = c;
                    CreateWizardButton(c.Name, () => OnClusterSelected(capturedCluster));
                }

                // Download segments in background
                var segments = await _repository.FetchProjectSegmentsByIdAsync(project.Id);
                Debug.Log($"Segmentos pre-descargados: {segments?.Count ?? 0}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error: {ex.Message}");
                CreateWizardButton("Error (Volver)", ShowProjects);
            }
        }

        private async void OnClusterSelected(Models.Cluster cluster)
        {
            _selectedCluster = cluster;
            if (welcomeText != null) welcomeText.text = "Cargando POPs...";
            ClearList();

            try
            {
                _currentPops = await _repository.FetchPopsByClusterIdAsync(cluster.Id);

                if (welcomeText != null) welcomeText.text = $"Cluster: {cluster.Name}\nSelecciona un POP";
                
                if (_currentPops == null || _currentPops.Count == 0)
                {
                    CreateWizardButton("Sin POPs (Volver)", () => OnProjectSelected(_selectedProject));
                    return;
                }

                CreateWizardButton("← Volver a Clusters", () => OnProjectSelected(_selectedProject)).GetComponent<Image>().color = new Color32(100, 30, 30, 255);

                foreach (var pop in _currentPops)
                {
                    var capturedPop = pop;
                    CreateWizardButton(pop.Name, () => OnPopSelected(capturedPop));
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error: {ex.Message}");
                CreateWizardButton("Error (Volver)", () => OnProjectSelected(_selectedProject));
            }
        }

        private void OnPopSelected(Models.Pop pop)
        {
            _selectedPop = pop;
            if (welcomeText != null) welcomeText.text = $"¡Todo Listo!\n{pop.Name}";
            ClearList();

            var startBtn = CreateWizardButton("CARGAR MUNDO 3D", () => 
            {
                if (welcomeText != null) welcomeText.text = "Cargando Cesium...";
                ClearList();
                // Aquí integraremos Cesium
            });

            // Make the final button green and massive
            startBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(450, 100);
            startBtn.GetComponent<Image>().color = new Color32(0, 150, 50, 255);
            startBtn.GetComponentInChildren<TextMeshProUGUI>().fontSize = 32;

            CreateWizardButton("← Cambiar POP", () => OnClusterSelected(_selectedCluster));
        }
    }

    // --- NUEVO SCRIPT DINÁMICO ---
    // Este script se encarga de posicionar, rotar y estirar el cilindro CADA FRAME.
    // Así, sin importar cuánto tarde Cesium en cargar las coordenadas de la Tierra, 
    // el cilindro siempre conectará el Punto A y el Punto B a la perfección.
    public class SegmentConnector : MonoBehaviour
    {
        public Transform PointA;
        public Transform PointB;
        public float Thickness = 0.5f;

        void Update()
        {
            if (PointA == null || PointB == null) return;
            
            Vector3 posA = PointA.position;
            Vector3 posB = PointB.position;
            float distance = Vector3.Distance(posA, posB);
            
            if (distance < 0.01f) 
            {
                // Si Cesium aún no ha calculado la distancia real, lo escondemos
                transform.localScale = Vector3.zero;
                return;
            }

            // Posición en el medio de los dos puntos
            transform.position = (posA + posB) / 2f;
            
            // Estirarlo para que mida exactamente la distancia (los cilindros en Unity miden 2 de alto por defecto)
            transform.localScale = new Vector3(Thickness, distance / 2f, Thickness);
            
            // Rotarlo para que apunte del Punto A al Punto B
            Vector3 direction = (posB - posA).normalized;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(90, 0, 0);
            }
        }
    }

    // --- SCRIPT PARA POSICIONAR NODOS GPS (POPs y NVTs) ---
    // Espera a que CesiumGlobeAnchor calcule la posición real antes de mostrar el cubo.
    public class NodePositioner : MonoBehaviour
    {
        public CesiumForUnity.CesiumGlobeAnchor Anchor;
        public Vector3 NodeScale = new Vector3(5f, 5f, 5f);

        private bool _positioned = false;

        void Update()
        {
            if (_positioned) return;
            if (Anchor == null) return;

            Vector3 pos = transform.position;
            float distFromOrigin = pos.magnitude;

            // Cuando Cesium calcule la posición GPS real (ya no estará en 0,0,0)
            if (distFromOrigin > 0.5f)
            {
                transform.localScale = NodeScale;
                _positioned = true;
                Debug.Log($"[Cesium] Nodo '{gameObject.name}' posicionado correctamente.");
            }
        }
    }

    /// <summary>
    /// Utilidad simple para hacer que el icono (como la pala) rote sobre su propio eje.
    /// </summary>
    public class IconRotator : MonoBehaviour
    {
        public float rotationSpeed = 30f;
        
        void Update()
        {
            // Rota sobre el eje Y global/local suavemente
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
        }
    }

    /// <summary>
    /// Escala dinámicamente un objeto cuando el usuario lo mira (basado en la dirección de la cámara).
    /// </summary>
    public class GazeScaler : MonoBehaviour
    {
        public Vector3 baseScale = new Vector3(300f, 300f, 300f);
        public Vector3 hoverScale = new Vector3(1000f, 1000f, 1000f);
        public float scaleSpeed = 8f;
        public float lookThreshold = 0.96f; // Aprox 15 grados de cono de visión

        private Transform _cameraTransform;

        void Start()
        {
            if (Camera.main != null) _cameraTransform = Camera.main.transform;
            transform.localScale = baseScale;
        }

        void Update()
        {
            if (_cameraTransform == null) return;

            Vector3 toTarget = (transform.position - _cameraTransform.position).normalized;
            float dot = Vector3.Dot(_cameraTransform.forward, toTarget);

            // Si el dot product es mayor al threshold, estamos mirando hacia el objeto
            Vector3 targetScale = (dot > lookThreshold) ? hoverScale : baseScale;
            
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleSpeed);
        }
    }
}
