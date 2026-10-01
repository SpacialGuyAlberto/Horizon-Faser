using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace HorizonFaser.UI
{
    /// <summary>
    /// Controlador para la tarjeta de información (Information Card) Premium FAANG Style
    /// </summary>
    public class NodeInfoCardUI : MonoBehaviour
    {
        [Header("UI Elements")]
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI typeText;
        public TextMeshProUGUI detailsText;
        public Image accentLine;
        public Image badgeBg;

        private Vector3 _baseScale = new Vector3(0.0025f, 0.0025f, 0.0025f);
        private float _currentAnimFactor = 0f;
        private Transform _cameraTransform;

        private void Awake()
        {
            if (Camera.main != null) _cameraTransform = Camera.main.transform;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_cameraTransform != null)
            {
                // 1. Billboard: Siempre mirar a la cámara
                Vector3 toCamera = transform.position - _cameraTransform.position;
                transform.rotation = Quaternion.LookRotation(toCamera);

                // 2. Escala Dinámica por Distancia: Para que sea legible a 100km o a 1 metro.
                float distance = toCamera.magnitude;
                // Si está a 2 metros, el multiplicador es 1. A 20 metros, será 10.
                float distanceMultiplier = Mathf.Max(1f, distance / 2f);
                
                // Aplicamos la escala base * multiplicador de distancia * factor de animación
                transform.localScale = _baseScale * distanceMultiplier * _currentAnimFactor;
            }
        }

        public void ShowCard(string nodeType, string nodeName, string details, Vector3 spawnPosition)
        {
            gameObject.SetActive(true);

            if (titleText != null) titleText.text = nodeName;
            if (typeText != null) typeText.text = nodeType.ToUpper();
            if (detailsText != null) detailsText.text = details;

            // Colores dinámicos estilo FAANG
            Color popColor = new Color(1f, 0.35f, 0.1f); // Vibrant Orange
            Color nvtColor = new Color(0.9f, 0.8f, 0.1f); // Vibrant Yellow
            Color segColor = new Color(0.1f, 0.6f, 1f); // Sleek Blue

            Color currentAccent = segColor;
            if (nodeType.Contains("Pop") || nodeType.Contains("POP")) currentAccent = popColor;
            else if (nodeType.Contains("Netz") || nodeType.Contains("NVT")) currentAccent = nvtColor;

            if (accentLine != null) accentLine.color = currentAccent;
            if (badgeBg != null)
            {
                Color badgeCol = currentAccent;
                badgeCol.a = 0.15f; // Transparencia sutil
                badgeBg.color = badgeCol;
                typeText.color = currentAccent; 
            }

            // Posicionar ligeramente arriba del nodo
            transform.position = spawnPosition + Vector3.up * 1.5f;

            StopAllCoroutines();
            StartCoroutine(AnimatePopIn());
        }

        public void HideCard()
        {
            if (!gameObject.activeInHierarchy) return;
            
            StopAllCoroutines();
            StartCoroutine(AnimatePopOut());
        }

        private IEnumerator AnimatePopIn()
        {
            float t = 0;
            float duration = 0.4f;
            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                // Spring curve
                _currentAnimFactor = 1f - Mathf.Cos(t * Mathf.PI * 2f) * Mathf.Exp(-t * 8f);
                yield return null;
            }
            _currentAnimFactor = 1f;
        }

        private IEnumerator AnimatePopOut()
        {
            float t = 0;
            float duration = 0.15f;
            float startFactor = _currentAnimFactor;
            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                _currentAnimFactor = startFactor * (1f - (t * t)); // Ease out
                yield return null;
            }
            _currentAnimFactor = 0f;
            gameObject.SetActive(false);
        }

        public static NodeInfoCardUI CreateGeneratedCard()
        {
            // 1. Canvas World Space
            GameObject canvasObj = new GameObject("Premium_NodeInfoCard");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            RectTransform canvasRT = canvasObj.GetComponent<RectTransform>();
            canvasRT.sizeDelta = new Vector2(450, 220); // Más ancho, proporciones cinemáticas

            // 2. Main Background (Glassmorphism oscuro)
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform, false);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.08f, 0.09f, 0.11f, 0.95f); // Gris muy oscuro, casi negro, elegante
            
            RectTransform bgRT = bgObj.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = Vector2.zero;

            // Drop Shadow falso para dar profundidad
            Shadow shadow = bgObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.5f);
            shadow.effectDistance = new Vector2(0, -5);

            // 3. Accent Line (Línea decorativa a la izquierda)
            GameObject accentObj = new GameObject("AccentLine");
            accentObj.transform.SetParent(bgObj.transform, false);
            Image accent = accentObj.AddComponent<Image>();
            accent.color = Color.white;
            
            RectTransform accentRT = accentObj.GetComponent<RectTransform>();
            accentRT.anchorMin = new Vector2(0, 0); accentRT.anchorMax = new Vector2(0, 1);
            accentRT.pivot = new Vector2(0, 0.5f);
            accentRT.sizeDelta = new Vector2(6, 0); // 6 pixeles de grosor

            // 4. Badge del Tipo (Pill shape background)
            GameObject badgeObj = new GameObject("BadgeBg");
            badgeObj.transform.SetParent(bgObj.transform, false);
            Image badge = badgeObj.AddComponent<Image>();
            badge.color = new Color(1, 1, 1, 0.15f);
            
            RectTransform badgeRT = badgeObj.GetComponent<RectTransform>();
            badgeRT.anchorMin = new Vector2(0, 1); badgeRT.anchorMax = new Vector2(0, 1);
            badgeRT.pivot = new Vector2(0, 1);
            badgeRT.anchoredPosition = new Vector2(25, -20);
            badgeRT.sizeDelta = new Vector2(150, 30);

            // Texto dentro del Badge
            GameObject typeObj = new GameObject("TypeText");
            typeObj.transform.SetParent(badgeObj.transform, false);
            TextMeshProUGUI type = typeObj.AddComponent<TextMeshProUGUI>();
            type.text = "NETWORK NODE";
            type.fontSize = 14;
            type.fontStyle = FontStyles.Bold;
            type.alignment = TextAlignmentOptions.Center;
            type.color = Color.white;
            
            RectTransform typeRT = typeObj.GetComponent<RectTransform>();
            typeRT.anchorMin = Vector2.zero; typeRT.anchorMax = Vector2.one;
            typeRT.sizeDelta = Vector2.zero;

            // 5. Texto del Título
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(bgObj.transform, false);
            TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
            title.text = "Identificador del Nodo";
            title.fontSize = 32;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Left;
            title.color = Color.white;
            
            RectTransform titleRT = titleObj.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0, 1); titleRT.anchorMax = new Vector2(1, 1);
            titleRT.pivot = new Vector2(0, 1);
            titleRT.anchoredPosition = new Vector2(25, -60);
            titleRT.sizeDelta = new Vector2(-40, 45);

            // 6. Texto de Detalles
            GameObject detailsObj = new GameObject("Details");
            detailsObj.transform.SetParent(bgObj.transform, false);
            TextMeshProUGUI details = detailsObj.AddComponent<TextMeshProUGUI>();
            details.text = "Latitud:\nLongitud:\nDistancia:";
            details.fontSize = 18;
            details.color = new Color(0.7f, 0.75f, 0.8f, 1f); // Gris claro azulado
            details.alignment = TextAlignmentOptions.TopLeft;
            
            RectTransform detailsRT = detailsObj.GetComponent<RectTransform>();
            detailsRT.anchorMin = new Vector2(0, 0); detailsRT.anchorMax = new Vector2(1, 0);
            detailsRT.pivot = new Vector2(0, 0);
            detailsRT.anchoredPosition = new Vector2(25, 20);
            detailsRT.sizeDelta = new Vector2(-40, 95);

            // 7. Ensamblar Script
            NodeInfoCardUI uiScript = canvasObj.AddComponent<NodeInfoCardUI>();
            uiScript.titleText = title;
            uiScript.typeText = type;
            uiScript.detailsText = details;
            uiScript.accentLine = accent;
            uiScript.badgeBg = badge;

            canvasObj.SetActive(false);
            return uiScript;
        }
    }
}
