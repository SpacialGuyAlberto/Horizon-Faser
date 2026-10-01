using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HorizonFaser.UI
{
    /// <summary>
    /// Controlador para la tarjeta de información (Information Card)
    /// que aparece al hacer clic en un nodo de red (Pop, Netzverteiler, Segmento).
    /// </summary>
    public class NodeInfoCardUI : MonoBehaviour
    {
        [Header("UI Elements")]
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI typeText;
        public TextMeshProUGUI detailsText;

        private void Awake()
        {
            // Ocultar por defecto
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Muestra la tarjeta con la información dada.
        /// </summary>
        public void ShowCard(string nodeType, string nodeName, string details, Vector3 spawnPosition)
        {
            gameObject.SetActive(true);

            if (titleText != null) titleText.text = nodeName;
            if (typeText != null) typeText.text = nodeType;
            if (detailsText != null) detailsText.text = details;

            // Posicionamos la tarjeta ligeramente por encima del nodo
            transform.position = spawnPosition + Vector3.up * 0.2f;
        }

        public void HideCard()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Crea una Card de UI de cero por código, útil si no quieres hacerlo manualmente en el Editor.
        /// Retorna el componente NodeInfoCardUI.
        /// </summary>
        public static NodeInfoCardUI CreateGeneratedCard()
        {
            // 1. Crear Canvas World Space
            GameObject canvasObj = new GameObject("Generated_NodeInfoCard");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // Si el proyecto usa BillboardUI para orientarse a la cámara, lo añadimos
            if (canvasObj.GetComponent<BillboardUI>() == null)
            {
                canvasObj.AddComponent<BillboardUI>();
            }

            RectTransform canvasRT = canvasObj.GetComponent<RectTransform>();
            canvasRT.sizeDelta = new Vector2(400, 200);
            canvasRT.localScale = new Vector3(0.001f, 0.001f, 0.001f); // Tamaño apropiado para AR/VR

            // 2. Crear panel de fondo
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform, false);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.85f); // Gris oscuro semi-transparente
            
            RectTransform bgRT = bgObj.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = Vector2.zero;

            // 3. Texto del Título
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(bgObj.transform, false);
            TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
            title.text = "Nombre";
            title.fontSize = 32;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.TopLeft;
            title.color = Color.white;
            
            RectTransform titleRT = titleObj.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0, 1);
            titleRT.anchorMax = new Vector2(1, 1);
            titleRT.pivot = new Vector2(0.5f, 1);
            titleRT.anchoredPosition = new Vector2(10, -10);
            titleRT.sizeDelta = new Vector2(-20, 40);

            // 4. Texto del Tipo
            GameObject typeObj = new GameObject("Type");
            typeObj.transform.SetParent(bgObj.transform, false);
            TextMeshProUGUI type = typeObj.AddComponent<TextMeshProUGUI>();
            type.text = "Tipo";
            type.fontSize = 20;
            type.fontStyle = FontStyles.Italic;
            type.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            
            RectTransform typeRT = typeObj.GetComponent<RectTransform>();
            typeRT.anchorMin = new Vector2(0, 1);
            typeRT.anchorMax = new Vector2(1, 1);
            typeRT.pivot = new Vector2(0.5f, 1);
            typeRT.anchoredPosition = new Vector2(10, -45);
            typeRT.sizeDelta = new Vector2(-20, 30);

            // 5. Texto de Detalles
            GameObject detailsObj = new GameObject("Details");
            detailsObj.transform.SetParent(bgObj.transform, false);
            TextMeshProUGUI details = detailsObj.AddComponent<TextMeshProUGUI>();
            details.text = "Detalles...";
            details.fontSize = 18;
            details.alignment = TextAlignmentOptions.TopLeft;
            details.color = Color.white;
            
            RectTransform detailsRT = detailsObj.GetComponent<RectTransform>();
            detailsRT.anchorMin = new Vector2(0, 0);
            detailsRT.anchorMax = new Vector2(1, 0);
            detailsRT.pivot = new Vector2(0.5f, 0);
            detailsRT.anchoredPosition = new Vector2(10, 10);
            detailsRT.sizeDelta = new Vector2(-20, 100);

            // 6. Ensamblar script
            NodeInfoCardUI uiScript = canvasObj.AddComponent<NodeInfoCardUI>();
            uiScript.titleText = title;
            uiScript.typeText = type;
            uiScript.detailsText = details;

            // Lo desactivamos para que empiece oculto
            canvasObj.SetActive(false);

            return uiScript;
        }
    }
}
