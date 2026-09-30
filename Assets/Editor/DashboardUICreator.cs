using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using HorizonFaser.UI;

namespace HorizonFaser.EditorTools
{
    public class DashboardUICreator : Editor
    {
        [MenuItem("Tools/Horizon/Crear Dashboard UI")]
        public static void CreateDashboardUI()
        {
            // 1. Crear el Canvas del Dashboard
            GameObject canvasObj = new GameObject("DashboardCanvas_Horizon");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10;
            
            canvasObj.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>(); // Soporte XR

            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(900, 700);
            canvasRect.position = new Vector3(0, 1.5f, 1.5f);
            canvasRect.localScale = new Vector3(0.002f, 0.002f, 0.002f);

            // 2. Crear Panel de fondo (efecto cristal oscuro)
            GameObject panelObj = new GameObject("BackgroundPanel");
            panelObj.transform.SetParent(canvasObj.transform, false);
            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0.02f, 0.02f, 0.05f, 0.85f); 
            
            RectTransform panelRect = panelObj.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;

            // 3. Crear textos y botones
            GameObject welcomeObj = CreateText("WelcomeText", "Bienvenido a Horizon", panelObj.transform, new Vector2(0, 250), 36);
            GameObject statsObj = CreateText("StatsText", "Cargando estadísticas...", panelObj.transform, new Vector2(0, 100), 24);
            
            GameObject cesiumBtnObj = CreateButton("CesiumButton", "Cargar Mundo 3D (Cesium)", panelObj.transform, new Vector2(0, -50), new Color(0.1f, 0.6f, 0.2f, 1f));
            GameObject logoutBtnObj = CreateButton("LogoutButton", "Cerrar Sesión", panelObj.transform, new Vector2(0, -150), new Color(0.8f, 0.2f, 0.2f, 1f));

            // 4. Configurar controlador
            DashboardUIController controller = canvasObj.AddComponent<DashboardUIController>();
            controller.welcomeText = welcomeObj.GetComponent<TextMeshProUGUI>();
            controller.statsText = statsObj.GetComponent<TextMeshProUGUI>();
            controller.openCesiumButton = cesiumBtnObj.GetComponent<Button>();
            controller.logoutButton = logoutBtnObj.GetComponent<Button>();

            // 5. Ocultarlo por defecto (el Login lo activará)
            canvasObj.SetActive(false);

            // 6. Conectarlo automáticamente al Login Canvas si existe en la escena
            LoginUIController loginCtrl = FindObjectOfType<LoginUIController>(true);
            if (loginCtrl != null)
            {
                loginCtrl.dashboardUI = canvasObj;
                Debug.Log("Dashboard UI creado y enlazado al LoginCanvas automáticamente.");
            }
            else
            {
                Debug.LogWarning("Dashboard creado, pero no se encontró el LoginCanvas para enlazarlo automáticamente.");
            }

            Selection.activeGameObject = canvasObj;
        }

        private static GameObject CreateText(string name, string text, Transform parent, Vector2 anchoredPosition, float fontSize)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);
            
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.color = Color.white;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            
            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(800, 100);
            rect.anchoredPosition = anchoredPosition;

            return textObj;
        }

        private static GameObject CreateButton(string name, string label, Transform parent, Vector2 anchoredPosition, Color color)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            
            Image bg = btnObj.AddComponent<Image>();
            bg.color = color;
            
            Button btn = btnObj.AddComponent<Button>();
            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 70);
            rect.anchoredPosition = anchoredPosition;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.color = Color.white;
            tmp.fontSize = 28;
            tmp.alignment = TextAlignmentOptions.Center;
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            return btnObj;
        }
    }
}
