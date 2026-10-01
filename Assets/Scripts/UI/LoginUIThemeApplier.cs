using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HorizonFaser.UI
{
    /// <summary>
    /// Maneja exclusivamente la responsabilidad de aplicar estilos visuales a la UI de Login (SRP).
    /// Separa la estética de la lógica de negocio en fragmentos pequeños y legibles.
    /// </summary>
    public static class LoginUIThemeApplier
    {
        private static Sprite _proceduralRoundedSprite;

        // --- PALETA DE COLORES ---
        private static readonly Color32 BgTopColor = new Color32(31, 31, 34, 255);      
        private static readonly Color32 BgBottomColor = new Color32(18, 18, 20, 255);   
        private static readonly Color32 CardColor = new Color32(30, 30, 32, 255);     
        private static readonly Color32 InputOutlineColor = new Color32(63, 63, 70, 255);  
        private static readonly Color32 PrimaryButtonColor = new Color32(255, 107, 0, 255);  
        private static readonly Color32 PrimaryButtonTextColor = new Color32(20, 20, 22, 255);    
        private static readonly Color32 TextWhite = new Color32(245, 245, 245, 255);  
        private static readonly Color32 TextMuted = new Color32(150, 150, 155, 255);  

        public static void ApplyPremiumDarkStyle(LoginUIController controller)
        {
            if (controller == null) return;

            Sprite roundedSprite = GetRoundedSprite();

            SetupPanelBackground(controller, roundedSprite);
            SetupInputFields(controller, roundedSprite);
            SetupLoginButton(controller.loginButton, roundedSprite);
            SetupStatusText(controller.statusText);
        }

        private static void SetupPanelBackground(LoginUIController controller, Sprite roundedSprite)
        {
            Image panel = EnsurePanelExists(controller);

            panel.color = Color.white;
            panel.sprite = roundedSprite;
            panel.type = Image.Type.Sliced;
            panel.pixelsPerUnitMultiplier = 1f; 
            
            RemoveComponent<Outline>(panel.gameObject);
            ApplyVerticalGradient(panel.gameObject, BgTopColor, BgBottomColor);
        }

        private static Image EnsurePanelExists(LoginUIController controller)
        {
            Image panel = controller.GetComponent<Image>();
            if (panel == null)
            {
                GameObject bgObj = new GameObject("SolidBackground");
                bgObj.transform.SetParent(controller.transform, false);
                bgObj.transform.SetAsFirstSibling();
                
                panel = bgObj.AddComponent<Image>();
                RectTransform rt = bgObj.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = new Vector2(50, 50);
            }
            return panel;
        }

        private static void ApplyVerticalGradient(GameObject obj, Color32 top, Color32 bottom)
        {
            var gradient = obj.GetComponent<UIGradient>();
            if (gradient == null) gradient = obj.AddComponent<UIGradient>();
            gradient.topColor = top;
            gradient.bottomColor = bottom;
        }

        private static void SetupInputFields(LoginUIController controller, Sprite roundedSprite)
        {
            TMP_InputField[] inputs = { controller.emailInput, controller.passwordInput };
            foreach (var input in inputs)
            {
                if (input == null || input.targetGraphic == null) continue;

                Image inputImg = input.targetGraphic as Image;
                if (inputImg != null)
                {
                    inputImg.color = CardColor;
                    inputImg.sprite = roundedSprite;
                    inputImg.type = Image.Type.Sliced;
                    inputImg.pixelsPerUnitMultiplier = 1f; 
                }
                
                ApplyOutline(input.targetGraphic.gameObject, InputOutlineColor, new Vector2(1, -1));

                if (input.textComponent != null) input.textComponent.color = TextWhite;
                if (input.placeholder != null) input.placeholder.color = TextMuted; 
            }
        }

        private static void ApplyOutline(GameObject obj, Color32 color, Vector2 distance)
        {
            var outline = obj.GetComponent<Outline>();
            if (outline == null) outline = obj.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
        }

        private static void SetupLoginButton(Button button, Sprite roundedSprite)
        {
            if (button == null || button.targetGraphic == null) return;

            Image btnImg = button.targetGraphic as Image;
            if (btnImg != null)
            {
                btnImg.color = PrimaryButtonColor; 
                btnImg.sprite = roundedSprite;
                btnImg.type = Image.Type.Sliced;
                btnImg.pixelsPerUnitMultiplier = 1f; 
            }
            
            RemoveComponent<Outline>(button.gameObject);

            var btnText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null) 
            {
                btnText.color = PrimaryButtonTextColor;
                btnText.fontStyle = FontStyles.Bold;
            }
        }

        private static void SetupStatusText(TextMeshProUGUI statusText)
        {
            if (statusText != null)
            {
                statusText.color = TextMuted; 
            }
        }

        public static void AddQuitButton(Transform parent)
        {
            if (parent == null) return;

            GameObject quitBtnObj = new GameObject("QuitButton");
            quitBtnObj.transform.SetParent(parent, false);

            var rt = quitBtnObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-20, -20);
            rt.sizeDelta = new Vector2(120, 50);

            var img = quitBtnObj.AddComponent<Image>();
            img.color = new Color(0.6f, 0.1f, 0.1f, 1f); 

            var btn = quitBtnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => Application.Quit());

            AddTextToButton(quitBtnObj.transform, "✕ Salir");
        }

        private static void AddTextToButton(Transform buttonTransform, string textContent)
        {
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonTransform, false);
            var textRt = textObj.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;
            
            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = textContent;
            tmp.fontSize = 18;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
        }

        private static void RemoveComponent<T>(GameObject obj) where T : Component
        {
            var comp = obj.GetComponent<T>();
            if (comp != null) UnityEngine.Object.Destroy(comp);
        }

        private static Sprite GetRoundedSprite()
        {
            if (_proceduralRoundedSprite != null) return _proceduralRoundedSprite;
            
            int r = 16;
            int size = r * 2;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color32 white = new Color32(255, 255, 255, 255);
            Color32 clear = new Color32(255, 255, 255, 0);
            Color32[] colors = new Color32[size * size];
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - r + 0.5f;
                    float dy = y - r + 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist <= r - 1f) colors[y * size + x] = white;
                    else if (dist <= r) colors[y * size + x] = new Color32(255, 255, 255, (byte)(255 * (r - dist)));
                    else colors[y * size + x] = clear;
                }
            }
            tex.SetPixels32(colors);
            tex.Apply();
            
            _proceduralRoundedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
            return _proceduralRoundedSprite;
        }
    }
}
