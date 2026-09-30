using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HorizonFaser.UI
{
    [ExecuteInEditMode]
    public class TailwindStyleApplier : MonoBehaviour
    {
        [Header("Colores (Estilo Tailwind Dark Mode)")]
        public Color backgroundColor = new Color(0.06f, 0.06f, 0.09f, 1f); // bg-slate-900
        public Color inputColor = new Color(0.12f, 0.13f, 0.18f, 1f); // bg-slate-800
        public Color inputBorderColor = new Color(0.2f, 0.2f, 0.3f, 1f); // border-slate-700
        public Color buttonGradientStart = new Color(0.39f, 0.22f, 0.94f, 1f); // indigo-600
        public Color textColor = new Color(0.9f, 0.9f, 0.9f, 1f);

        [Header("Referencias a UI")]
        public Image backgroundImage;
        public TMP_InputField[] inputs;
        public Button[] buttons;
        public TextMeshProUGUI[] labels;

        public void ApplyStyles()
        {
            if (backgroundImage != null)
                backgroundImage.color = backgroundColor;

            foreach (var input in inputs)
            {
                if (input != null && input.targetGraphic != null)
                {
                    input.targetGraphic.color = inputColor;
                    if (input.textComponent != null) input.textComponent.color = textColor;
                }
            }

            foreach (var btn in buttons)
            {
                if (btn != null && btn.targetGraphic != null)
                {
                    btn.targetGraphic.color = buttonGradientStart;
                }
            }

            foreach (var label in labels)
            {
                if (label != null)
                    label.color = textColor;
            }
        }
    }
}
