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

                for (int i = 0; i < segment.Geometry.Count - 1; i++)
                {
                    var pA = segment.Geometry[i];
                    var pB = segment.Geometry[i+1];

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

                    // 3. Crear el cilindro dinámico
                    GameObject cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    cyl.name = $"Segment_{segment.Id}_Line_{i}";
                    cyl.transform.SetParent(segmentsParent.transform, true);
                    cyl.GetComponent<MeshRenderer>().material = segmentMaterial;
                    
                    // Añadir script de conexión dinámica para que se actualice CADA FRAME
                    var connector = cyl.AddComponent<SegmentConnector>();
                    connector.PointA = anchorObjA.transform;
                    connector.PointB = anchorObjB.transform;
                    connector.Thickness = 0.5f; // Medio metro de grosor para que se vea bien desde lejos
                    
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

            // Destruir el Canvas para que no tape la vista
            Destroy(gameObject);
        }

        private void RenderPopsAndNvtsCesium(List<Cluster> clusters)
        {
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
                    // POP = Cubo grande naranja (10x10x10 m)
                    GameObject popObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    popObj.name = $"POP_{pop.Id}";
                    // CRÍTICO: SetParent PRIMERO para que CesiumGlobeAnchor encuentre el Georeference
                    popObj.transform.SetParent(nodesParent.transform, false);
                    popObj.transform.localScale = new Vector3(10f, 10f, 10f);
                    popObj.GetComponent<MeshRenderer>().material = popMaterial;
                    // Añadir anchor DESPUÉS de que ya es hijo del Georeference
                    var popAnchor = popObj.AddComponent<CesiumForUnity.CesiumGlobeAnchor>();
                    popAnchor.longitudeLatitudeHeight = new Unity.Mathematics.double3(pop.Longitude, pop.Latitude, 5);
                    popCount++;

                    if (pop.Nvts == null) continue;

                    foreach (var nvt in pop.Nvts)
                    {
                        // NVT = Cubo pequeño amarillo (3x3x3 m)
                        GameObject nvtObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        nvtObj.name = $"NVT_{nvt.Id}";
                        // CRÍTICO: SetParent PRIMERO
                        nvtObj.transform.SetParent(nodesParent.transform, false);
                        nvtObj.transform.localScale = new Vector3(3f, 3f, 3f);
                        nvtObj.GetComponent<MeshRenderer>().material = nvtMaterial;
                        var nvtAnchor = nvtObj.AddComponent<CesiumForUnity.CesiumGlobeAnchor>();
                        nvtAnchor.longitudeLatitudeHeight = new Unity.Mathematics.double3(nvt.Longitude, nvt.Latitude, 2);
                        nvtCount++;
                    }
                }
            }

            Debug.Log($"[Cesium] Nodos: {popCount} POPs (naranja 10m) y {nvtCount} NVTs (amarillo 3m).");
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
}
