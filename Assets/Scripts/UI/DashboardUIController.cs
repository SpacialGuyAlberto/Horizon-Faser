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

            if (FindObjectOfType<HorizonFaser.XR.XRPointerMagnet>() == null)
            {
                GameObject magnet = new GameObject("XRPointerMagnet");
                magnet.AddComponent<HorizonFaser.XR.XRPointerMagnet>();
            }

            if (welcomeText != null)
            {
                welcomeText.text = "App lista. Esperando conexión del teléfono por UDP...";
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

            // EVALUACIÓN: ¿Hay un proyecto siendo transmitido (broadcasting)?
            var companionListener = FindObjectOfType<HorizonFaser.Sensors.ARCompanionListener>();
            if (companionListener != null && companionListener.latestData != null && companionListener.latestData.projectId > 0)
            {
                long broadcastedProjectId = companionListener.latestData.projectId;
                Debug.Log($"[Dashboard] Se encontró un proyecto siendo transmitido: {broadcastedProjectId}");
                
                var targetProject = _projects.Find(p => p.Id == broadcastedProjectId);
                if (targetProject != null)
                {
                    AutoLoadAndRenderProject(targetProject);
                }
                else
                {
                    // Si el proyecto transmitido no está en la lista inicial, lo creamos temporalmente para cargarlo
                    LoadProjectFromId(broadcastedProjectId);
                }
            }
            else
            {
                // Si NO hay proyecto transmitiéndose, mostramos pantalla de que no hay proyecto seleccionado
                Debug.Log("[Dashboard] No se detectó transmisión (broadcast) activa.");
                ShowNoProjectSelected();
            }
        }

        private void ShowNoProjectSelected()
        {
            if (welcomeText != null)
            {
                welcomeText.text = "No hay proyecto seleccionado.\nEsperando broadcast desde la App Móvil...";
                welcomeText.color = Color.white;
            }
            ClearList();
            
            // Opcional: Podrías mantener el botón de mostrar todos los proyectos por si quieren elegir manual
            CreateWizardButton("Ver Lista de Proyectos", ShowProjects);
        }

        public void LoadProjectFromId(long projectId)
        {
            if (_repository == null)
            {
                _repository = new Repositories.QuarkBauRemoteRepository();
                ((Repositories.QuarkBauRemoteRepository)_repository).Configure("http://fedora.fritz.box:8080", "");
            }
            
            Debug.Log($"[Dashboard] Loading project {projectId} requested by Mobile App via UDP.");
            var dummyProject = new Models.QuarkbauProject { Id = projectId };
            AutoLoadAndRenderProject(dummyProject);
        }

        public async void AutoLoadAndRenderProject(Models.QuarkbauProject project)
        {
            var canvas = GetComponent<Canvas>();
            
            if (_listContainer != null) _listContainer.SetActive(false);
            
            if (welcomeText != null) {
                welcomeText.text = $"Descargando datos del proyecto {project.Id}...";
                welcomeText.color = Color.yellow;
            }

            var segments = await _repository.FetchProjectSegmentsByIdAsync(project.Id);
            
            // 1. Calcular centro del proyecto para validación de distancia (usando segmentos si existen)
            double minLat = double.MaxValue, maxLat = double.MinValue;
            double minLng = double.MaxValue, maxLng = double.MinValue;
            bool hasValidCoords = false;

            if (segments != null && segments.Count > 0)
            {
                foreach (var seg in segments) {
                    if (seg.Geometry != null) {
                        foreach (var pt in seg.Geometry) {
                            if (pt.Latitude < minLat) minLat = pt.Latitude;
                            if (pt.Latitude > maxLat) maxLat = pt.Latitude;
                            if (pt.Longitude < minLng) minLng = pt.Longitude;
                            if (pt.Longitude > maxLng) maxLng = pt.Longitude;
                            hasValidCoords = true;
                        }
                    }
                }
            }

            if (hasValidCoords) {
                double projLat = (minLat + maxLat) / 2.0;
                double projLng = (minLng + maxLng) / 2.0;
                
                var companionListener = FindObjectOfType<HorizonFaser.Sensors.ARCompanionListener>();
                if (companionListener != null && companionListener.latestData != null && companionListener.latestData.lat != 0) {
                    double phoneLat = companionListener.latestData.lat;
                    double phoneLng = companionListener.latestData.lng;
                    
                    // Aproximación rápida a kilómetros usando Haversine plano
                    double latDiff = (projLat - phoneLat) * 111.32;
                    double lngDiff = (projLng - phoneLng) * 111.32 * System.Math.Cos(projLat * System.Math.PI / 180.0);
                    double distanceKm = System.Math.Sqrt(latDiff * latDiff + lngDiff * lngDiff);

                    Debug.Log($"[Dashboard] Distancia al proyecto: {distanceKm:F2} km");

                    if (distanceKm > 10.0) // Límite de 10 km
                    {
                        if (welcomeText != null) {
                            welcomeText.text = $"ESTÁS MUY LEJOS\nDistancia al proyecto: {distanceKm:F1} km. Acércate para renderizar.";
                            welcomeText.color = Color.red;
                        }
                        if (canvas != null) canvas.enabled = true; // Asegurar que sea visible el mensaje
                        return; // ABORTAR RENDERIZADO POR DISTANCIA
                    }
                }
            }

            if (canvas != null) canvas.enabled = false;
            
            bool renderedSomething = false;

            // 2. Renderizar Segmentos (si hay)
            if (segments != null && segments.Count > 0)
            {
                _ = HorizonFaser.Renderers.SegmentRenderer.RenderSegmentsCesium(segments, GetComponent<CanvasGroup>(), FindObjectOfType<CesiumForUnity.CesiumGeoreference>(), _repository);
                renderedSomething = true;
            }
            else
            {
                Debug.Log($"[Dashboard] Proyecto {project.Id} no tiene segmentos.");
            }

            try
            {
                var pops = await _repository.FetchProjectPopsAsync(project.Id);
                var nvts = await _repository.FetchProjectNetzverteilersAsync(project.Id);
                HorizonFaser.Renderers.NodeRenderer.RenderPopsAndNvtsCesium(pops, nvts, FindObjectOfType<CesiumForUnity.CesiumGeoreference>());
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Cesium] No se pudieron cargar los POPs o NVTs: {ex.Message}");
            }

            try
            {
                var utilities = await _repository.FetchExternalUtilitiesAsync();
                if (utilities != null && utilities.Count > 0)
                {
                    _ = HorizonFaser.Renderers.UtilityRenderer.RenderUtilitiesCesium(utilities, FindObjectOfType<CesiumForUnity.CesiumGeoreference>());
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Cesium] No se pudieron cargar las utilidades externas: {ex.Message}");
            }
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
}
