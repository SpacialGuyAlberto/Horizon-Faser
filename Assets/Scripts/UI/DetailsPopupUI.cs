using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HorizonFaser.UI
{
    public class DetailsPopupUI : MonoBehaviour
    {
        private static GameObject _detailsPopup;
        private static RawImage _popupImage;

        public static void OpenDetailsPopup(MonoBehaviour coroutineRunner, string title, string info, string imageUrl)
        {
            if (_detailsPopup == null)
            {
                _detailsPopup = CreateDetailsPopup();
            }

            TextMeshProUGUI[] texts = _detailsPopup.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in texts)
            {
                if (t.name == "PopupTitleText") t.text = title;
                if (t.name == "PopupDetailsText") t.text = info;
            }

            if (_popupImage != null)
            {
                _popupImage.texture = null; // clear old image
                if (imageUrl.StartsWith("http"))
                {
                    coroutineRunner.StartCoroutine(HttpImageDownloader.LoadImageFromUrl(imageUrl, _popupImage));
                }
                else
                {
                    Texture2D tex = Resources.Load<Texture2D>(imageUrl);
                    if (tex != null) _popupImage.texture = tex;
                }
            }

            _detailsPopup.SetActive(true);

            // Position it 2 meters in front of the camera safely
            if (Camera.main != null)
            {
                Vector3 camPos = Camera.main.transform.position;
                Vector3 forward = Camera.main.transform.forward;
                
                Vector3 horizontalForward = new Vector3(forward.x, 0, forward.z);
                if (horizontalForward.sqrMagnitude > 0.01f)
                    horizontalForward.Normalize();
                else
                    horizontalForward = forward.normalized;
                
                _detailsPopup.transform.position = camPos + horizontalForward * 2.0f;
                
                Vector3 toCamera = _detailsPopup.transform.position - camPos;
                _detailsPopup.transform.rotation = Quaternion.LookRotation(toCamera);
            }
        }

        public static void HideDetailsPopup()
        {
            if (_detailsPopup != null)
                _detailsPopup.SetActive(false);
        }

        public static void SetScale(Vector3 scale)
        {
            if (_detailsPopup != null) _detailsPopup.transform.localScale = scale;
        }

        private static GameObject CreateDetailsPopup()
        {
            GameObject canvasObj = new GameObject("Details_Popup");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            try { canvasObj.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>(); } catch { }

            if (Camera.main != null) canvas.worldCamera = Camera.main;

            RectTransform canvasRT = canvasObj.GetComponent<RectTransform>();
            canvasRT.sizeDelta = new Vector2(800, 450); // Modal apaisado
            canvasObj.transform.localScale = new Vector3(0.003f, 0.003f, 0.003f);

            // Background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform, false);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.sprite = UISpriteFactory.GetRoundedSprite();
            bgImage.type = Image.Type.Sliced;
            bgImage.color = new Color(0.11f, 0.11f, 0.12f, 0.98f);
            RectTransform bgRT = bgObj.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = Vector2.zero;

            // Title
            GameObject titleObj = new GameObject("PopupTitleText");
            titleObj.transform.SetParent(bgObj.transform, false);
            TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
            title.text = "TITULO";
            title.fontSize = 32;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.color = Color.white;
            RectTransform titleRT = titleObj.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0, 1); titleRT.anchorMax = new Vector2(1, 1);
            titleRT.pivot = new Vector2(0.5f, 1);
            titleRT.anchoredPosition = new Vector2(0, -20);
            titleRT.sizeDelta = new Vector2(-40, 40);

            // Image (Left Side)
            GameObject imgObj = new GameObject("PopupImage");
            imgObj.transform.SetParent(bgObj.transform, false);
            _popupImage = imgObj.AddComponent<RawImage>();
            _popupImage.color = new Color(0.2f, 0.2f, 0.2f, 1f); 
            RectTransform imgRT = imgObj.GetComponent<RectTransform>();
            imgRT.anchorMin = new Vector2(0, 0); imgRT.anchorMax = new Vector2(0, 1);
            imgRT.pivot = new Vector2(0, 0.5f);
            imgRT.anchoredPosition = new Vector2(30, -10);
            imgRT.sizeDelta = new Vector2(300, -120);

            // Text Info (Right Side)
            GameObject detailsObj = new GameObject("PopupDetailsText");
            detailsObj.transform.SetParent(bgObj.transform, false);
            TextMeshProUGUI details = detailsObj.AddComponent<TextMeshProUGUI>();
            details.text = "Cargando datos...";
            details.fontSize = 28;
            details.color = Color.white;
            details.alignment = TextAlignmentOptions.TopLeft;
            details.textWrappingMode = TextWrappingModes.Normal;
            details.overflowMode = TextOverflowModes.Overflow;
            RectTransform detailsRT = detailsObj.GetComponent<RectTransform>();
            detailsRT.anchorMin = new Vector2(0, 0); detailsRT.anchorMax = new Vector2(1, 1);
            detailsRT.pivot = new Vector2(0.5f, 0.5f);
            detailsRT.anchoredPosition = new Vector2(180, -30); 
            detailsRT.sizeDelta = new Vector2(-380, -160);

            // Close Button
            GameObject btnObj = new GameObject("CloseButton");
            btnObj.transform.SetParent(bgObj.transform, false);
            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.sprite = UISpriteFactory.GetPillSprite();
            btnImg.type = Image.Type.Sliced;
            btnImg.color = new Color(0.17f, 0.17f, 0.18f, 1f);
            Button btn = btnObj.AddComponent<Button>();
            RectTransform btnRT = btnObj.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.5f, 0); btnRT.anchorMax = new Vector2(0.5f, 0);
            btnRT.pivot = new Vector2(0.5f, 0);
            btnRT.anchoredPosition = new Vector2(0, 30);
            btnRT.sizeDelta = new Vector2(200, 50);

            GameObject btnTextObj = new GameObject("BtnText");
            btnTextObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
            btnText.text = "CERRAR";
            btnText.fontSize = 20;
            btnText.fontStyle = FontStyles.Bold;
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.color = Color.white;
            RectTransform btnTextRT = btnTextObj.GetComponent<RectTransform>();
            btnTextRT.anchorMin = Vector2.zero; btnTextRT.anchorMax = Vector2.one;
            btnTextRT.sizeDelta = Vector2.zero;

            btn.onClick.AddListener(() => {
                canvasObj.SetActive(false);
            });

            XRInteractionHelper.SetLayerRecursively(canvasObj, 5); 
            XRInteractionHelper.MakeGrabbable(canvasObj, canvasRT.sizeDelta);
            canvasObj.SetActive(false);
            return canvasObj;
        }
    }
}
