using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;

namespace HorizonFaser.UI
{
    [Serializable]
    public class PermitData
    {
        public string id;
        public string name;
        public string previewImageUrl;
        public string pdfUrl;
    }

    public static class JsonHelper
    {
        public static T[] FromJson<T>(string json)
        {
            string newJson = "{ \"array\": " + json + "}";
            Wrapper<T> wrapper = UnityEngine.JsonUtility.FromJson<Wrapper<T>>(newJson);
            return wrapper.array;
        }

        [Serializable]
        public class Wrapper<T>
        {
            public T[] array;
        }
    }

    public class PermitPopupUI : MonoBehaviour
    {
        private static GameObject _permitPopup;
        private static string _currentPdfUrl;
        private static GameObject _openPdfBtnObj;

        public static void SetScale(Vector3 scale)
        {
            if (_permitPopup != null) _permitPopup.transform.localScale = scale;
        }

        public static void HidePermitPopup()
        {
            if (_permitPopup != null) _permitPopup.SetActive(false);
        }

        public static void OpenPermitPopup(MonoBehaviour coroutineRunner, long segmentId)
        {
            if (_permitPopup == null)
            {
                _permitPopup = CreatePermitPopup();
            }
            _permitPopup.SetActive(true);

            if (Camera.main != null)
            {
                Vector3 camPos = Camera.main.transform.position;
                Vector3 forward = Camera.main.transform.forward;
                
                Vector3 horizontalForward = new Vector3(forward.x, 0, forward.z);
                if (horizontalForward.sqrMagnitude > 0.01f)
                    horizontalForward.Normalize();
                else
                    horizontalForward = forward.normalized;
                
                _permitPopup.transform.position = camPos + horizontalForward * 1.5f;
                Vector3 toCamera = _permitPopup.transform.position - camPos;
                _permitPopup.transform.rotation = Quaternion.LookRotation(toCamera);
            }
            
            coroutineRunner.StartCoroutine(FetchAndDisplayPermit(coroutineRunner, segmentId));
        }

        private static IEnumerator FetchAndDisplayPermit(MonoBehaviour runner, long segmentId)
        {
            Transform bgTrans = _permitPopup.transform.Find("PermitImage");
            Transform loadingTrans = _permitPopup.transform.Find("LoadingText");
            if (loadingTrans != null) 
            {
                loadingTrans.gameObject.SetActive(true);
                loadingTrans.GetComponent<TextMeshProUGUI>().text = "Cargando documento...\nPor favor espera";
            }

            if (bgTrans != null)
            {
                RawImage bgImage = bgTrans.GetComponent<RawImage>();
                bgImage.texture = null;
                bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

#if UNITY_EDITOR
                string baseUrl = "http://fedora.fritz.box:8080";
#else
                string baseUrl = "http://fedora.fritz.box:8080";
#endif
                string url = $"{baseUrl}/api/v1/segments/{segmentId}/permits";
                
                Debug.Log("Fetching permits with HttpClient from: " + url);
                
                var fetchTask = System.Threading.Tasks.Task.Run(async () => {
                    using (System.Net.Http.HttpClient client = new System.Net.Http.HttpClient())
                    {
                        client.Timeout = System.TimeSpan.FromSeconds(60);
                        var response = await client.GetAsync(url);
                        response.EnsureSuccessStatusCode();
                        return await response.Content.ReadAsStringAsync();
                    }
                });

                while (!fetchTask.IsCompleted)
                {
                    yield return null;
                }

                if (fetchTask.IsFaulted)
                {
                    Debug.LogError("Failed to fetch permits: " + fetchTask.Exception?.Message);
                    if (loadingTrans != null) loadingTrans.GetComponent<TextMeshProUGUI>().text = "Error de red:\n" + fetchTask.Exception?.GetBaseException().Message;
                    yield break;
                }

                string json = fetchTask.Result;
                PermitData[] permits = null;
                try {
                    permits = JsonHelper.FromJson<PermitData>(json);
                } catch (Exception) {}

                if (permits == null || permits.Length == 0 || string.IsNullOrEmpty(permits[0].pdfUrl))
                {
                    int pdfIndex = json.IndexOf("\"pdfUrl\":\"", StringComparison.Ordinal);
                    if (pdfIndex > 0)
                    {
                        pdfIndex += 10;
                        int endQuote = json.IndexOf("\"", pdfIndex, StringComparison.Ordinal);
                        if (endQuote > pdfIndex)
                        {
                            _currentPdfUrl = json.Substring(pdfIndex, endQuote - pdfIndex);
                        }
                    }
                    
                    int imgIndex = json.IndexOf("\"previewImageUrl\":\"", StringComparison.Ordinal);
                    if (imgIndex > 0)
                    {
                        imgIndex += 19;
                        int endQuote = json.IndexOf("\"", imgIndex, StringComparison.Ordinal);
                        if (endQuote > imgIndex)
                        {
                            string imgUrl = json.Substring(imgIndex, endQuote - imgIndex);
                            if (permits == null || permits.Length == 0)
                            {
                                permits = new PermitData[] { new PermitData() { pdfUrl = _currentPdfUrl, previewImageUrl = imgUrl } };
                            }
                            else 
                            {
                                permits[0].previewImageUrl = imgUrl;
                            }
                        }
                    }
                }

                if (permits != null && permits.Length > 0)
                {
                    string imgUrl = permits[0].previewImageUrl;
                    
                    if (string.IsNullOrEmpty(_currentPdfUrl) && !string.IsNullOrEmpty(permits[0].pdfUrl))
                    {
                        _currentPdfUrl = permits[0].pdfUrl;
                    }
                    
#if !UNITY_EDITOR
                    if (!string.IsNullOrEmpty(_currentPdfUrl))
                    {
                        _currentPdfUrl = _currentPdfUrl.Replace("localhost", "fedora.fritz.box");
                    }
#endif
                    if (!string.IsNullOrEmpty(_currentPdfUrl))
                    {
                        _currentPdfUrl = Uri.EscapeUriString(_currentPdfUrl);
                        if (_openPdfBtnObj != null) _openPdfBtnObj.SetActive(true);
                    }

                    if (!string.IsNullOrEmpty(imgUrl))
                    {
#if !UNITY_EDITOR
                        imgUrl = imgUrl.Replace("localhost", "fedora.fritz.box");
#endif
                        imgUrl = Uri.EscapeUriString(imgUrl);
                        runner.StartCoroutine(HttpImageDownloader.LoadImageFromUrl(imgUrl, bgImage, loadingTrans));
                    }
                    else 
                    {
                        if (loadingTrans != null) loadingTrans.GetComponent<TextMeshProUGUI>().text = "No hay previsualización disponible";
                    }
                }
                else 
                {
                    if (loadingTrans != null) loadingTrans.GetComponent<TextMeshProUGUI>().text = "No se encontraron permisos";
                }
            }
        }

        private static GameObject CreatePermitPopup()
        {
            GameObject canvasObj = new GameObject("Permit_Popup");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            try { canvasObj.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>(); } catch { }

            if (Camera.main != null) canvas.worldCamera = Camera.main;

            RectTransform canvasRT = canvasObj.GetComponent<RectTransform>();
            canvasRT.sizeDelta = new Vector2(400, 620);
            canvasObj.transform.localScale = new Vector3(0.003f, 0.003f, 0.003f);

            GameObject bgObj = new GameObject("PermitImage");
            bgObj.transform.SetParent(canvasObj.transform, false);
            RawImage bgImage = bgObj.AddComponent<RawImage>();
            bgImage.color = new Color(0.11f, 0.11f, 0.12f, 0.95f);
            
            RectTransform bgRT = bgObj.GetComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0.5f, 1); bgRT.anchorMax = new Vector2(0.5f, 1);
            bgRT.pivot = new Vector2(0.5f, 1);
            bgRT.sizeDelta = new Vector2(400, 560);
            bgRT.anchoredPosition = Vector2.zero;

            GameObject loadingObj = new GameObject("LoadingText");
            loadingObj.transform.SetParent(canvasObj.transform, false);
            TextMeshProUGUI loadingText = loadingObj.AddComponent<TextMeshProUGUI>();
            loadingText.text = "Cargando documento...\nPor favor espera";
            loadingText.fontSize = 22;
            loadingText.fontStyle = FontStyles.Bold;
            loadingText.alignment = TextAlignmentOptions.Center;
            loadingText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            RectTransform loadingRT = loadingObj.GetComponent<RectTransform>();
            loadingRT.anchorMin = new Vector2(0.5f, 0.5f); loadingRT.anchorMax = new Vector2(0.5f, 0.5f);
            loadingRT.pivot = new Vector2(0.5f, 0.5f);
            loadingRT.anchoredPosition = new Vector2(0, 50);
            loadingRT.sizeDelta = new Vector2(350, 100);

            GameObject openBtnObj = new GameObject("OpenPdfButton");
            openBtnObj.transform.SetParent(canvasObj.transform, false);
            Image openBtnImg = openBtnObj.AddComponent<Image>();
            openBtnImg.sprite = UISpriteFactory.GetPillSprite();
            openBtnImg.type = Image.Type.Sliced;
            openBtnImg.color = new Color(0f, 0.48f, 1f, 1f);
            Button openBtn = openBtnObj.AddComponent<Button>();
            openBtnObj.SetActive(false); 
            
            RectTransform openBtnRT = openBtnObj.GetComponent<RectTransform>();
            openBtnRT.anchorMin = new Vector2(0.5f, 0); openBtnRT.anchorMax = new Vector2(0.5f, 0);
            openBtnRT.pivot = new Vector2(0.5f, 0);
            openBtnRT.anchoredPosition = new Vector2(0, 60);
            openBtnRT.sizeDelta = new Vector2(150, 40);

            GameObject openBtnTextObj = new GameObject("OpenBtnText");
            openBtnTextObj.transform.SetParent(openBtnObj.transform, false);
            TextMeshProUGUI openBtnText = openBtnTextObj.AddComponent<TextMeshProUGUI>();
            openBtnText.text = "VER PDF";
            openBtnText.fontSize = 20;
            openBtnText.fontStyle = FontStyles.Bold;
            openBtnText.alignment = TextAlignmentOptions.Center;
            openBtnText.color = Color.white;
            RectTransform openBtnTextRT = openBtnTextObj.GetComponent<RectTransform>();
            openBtnTextRT.anchorMin = Vector2.zero; openBtnTextRT.anchorMax = Vector2.one;
            openBtnTextRT.sizeDelta = Vector2.zero;
            
            openBtn.onClick.AddListener(() => {
                openBtnText.text = "ABRIENDO...";
                if (!string.IsNullOrEmpty(_currentPdfUrl))
                {
                    Application.OpenURL(_currentPdfUrl);
                }
                else
                {
                    openBtnText.text = "URL VACÍA";
                }
            });

            _openPdfBtnObj = openBtnObj;

            GameObject btnObj = new GameObject("CloseButton");
            btnObj.transform.SetParent(canvasObj.transform, false);
            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.sprite = UISpriteFactory.GetPillSprite();
            btnImg.type = Image.Type.Sliced;
            btnImg.color = new Color(0.17f, 0.17f, 0.18f, 1f);
            Button btn = btnObj.AddComponent<Button>();
            RectTransform btnRT = btnObj.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.5f, 0); btnRT.anchorMax = new Vector2(0.5f, 0);
            btnRT.pivot = new Vector2(0.5f, 0);
            btnRT.anchoredPosition = new Vector2(0, 10);
            btnRT.sizeDelta = new Vector2(150, 40);

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
