using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using HorizonFaser.UI;

namespace HorizonFaser.EditorTools
{
    public class LoginUICreator : Editor
    {
        [MenuItem("Tools/Horizon/Crear Login UI (Automático)")]
        public static void CreateLoginUI()
        {
            // 1. Crear el Canvas
            GameObject canvasObj = new GameObject("LoginCanvas_Horizon");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10;
            
            canvasObj.AddComponent<GraphicRaycaster>();

            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(800, 600);
            
            // Posicionarlo frente a la cámara (X:0, Y:1.5, Z:1.5)
            canvasRect.position = new Vector3(0, 1.5f, 1.5f);
            canvasRect.localScale = new Vector3(0.002f, 0.002f, 0.002f);

            // 2. Crear Panel semi-transparente (Estilo Meta Quest: Oscuro, casi opaco)
            GameObject panelObj = new GameObject("QuestStylePanel");
            panelObj.transform.SetParent(canvasObj.transform, false);
            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.11f, 0.95f); // Gris muy oscuro
            
            RectTransform panelRect = panelObj.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;

            // 3. Crear Textos y Botones
            GameObject titleObj = CreateText("Title", "Inicio de Sesión", panelObj.transform, new Vector2(0, 150), 32);
            GameObject emailObj = CreateInputField("EmailInput", panelObj.transform, new Vector2(0, 50), false, "Email...");
            GameObject passObj = CreateInputField("PasswordInput", panelObj.transform, new Vector2(0, -30), true, "Contraseña...");
            GameObject btnObj = CreateButton("LoginButton", "Entrar", panelObj.transform, new Vector2(0, -120), new Color(0.15f, 0.45f, 0.9f, 1f)); // Azul Quest
            GameObject statusObj = CreateText("StatusText", "Esperando credenciales", panelObj.transform, new Vector2(0, -200), 20);

            // 4. Añadir el script LoginUIController que creamos antes
            LoginUIController controller = canvasObj.AddComponent<LoginUIController>();
            controller.emailInput = emailObj.GetComponent<TMP_InputField>();
            controller.passwordInput = passObj.GetComponent<TMP_InputField>();
            controller.loginButton = btnObj.GetComponent<Button>();
            controller.statusText = statusObj.GetComponent<TextMeshProUGUI>();

            // Seleccionar en el editor para que el usuario lo vea
            Selection.activeGameObject = canvasObj;
            Debug.Log("¡Menú de Login Estilo Quest creado automáticamente! Dale al Play para probarlo.");
        }

        private static GameObject CreateInputField(string name, Transform parent, Vector2 anchoredPosition, bool isPassword, string placeholder)
        {
            GameObject inputObj = new GameObject(name);
            inputObj.transform.SetParent(parent, false);
            
            Image bg = inputObj.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.22f, 1f); // Fondo gris oscuro para el input
            
            TMP_InputField inputField = inputObj.AddComponent<TMP_InputField>();
            if (isPassword)
            {
                inputField.inputType = TMP_InputField.InputType.Password;
            }
            
            RectTransform rect = inputObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(450, 60);
            rect.anchoredPosition = anchoredPosition;

            // Componente de texto
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(inputObj.transform, false);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.color = Color.white; // Texto blanco
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.text = placeholder; // Texto inicial de placeholder
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            
            inputField.textComponent = tmp;

            return inputObj;
        }

        private static GameObject CreateButton(string name, string label, Transform parent, Vector2 anchoredPosition, Color color)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            
            Image bg = btnObj.AddComponent<Image>();
            bg.color = color;
            
            Button btn = btnObj.AddComponent<Button>();
            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(250, 60); // Botón más grande y fácil de tocar
            rect.anchoredPosition = anchoredPosition;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.color = Color.white;
            tmp.fontSize = 26;
            tmp.alignment = TextAlignmentOptions.Center;
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            return btnObj;
        }

        private static GameObject CreateText(string name, string text, Transform parent, Vector2 anchoredPosition, float fontSize)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);
            
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.color = new Color(0.8f, 0.8f, 0.85f, 1f); // Gris clarito para lectura en VR
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            
            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(600, 50);
            rect.anchoredPosition = anchoredPosition;

            return textObj;
        }
    }
}
