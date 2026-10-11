using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Threading.Tasks;
using System.Net.Http;
using System;
using HorizonFaser.UI; // For PermitData and JsonHelper

namespace HorizonFaser.Network
{
    public static class PermitNetworkService
    {
        public static IEnumerator FetchAndDisplayPermit(long segmentId, GameObject permitPopup, Action<string> onPdfReady)
        {
            Transform bgTrans = permitPopup.transform.Find("PermitImage");
            Transform loadingTrans = permitPopup.transform.Find("LoadingText");
            
            if (loadingTrans != null) 
            {
                loadingTrans.gameObject.SetActive(true);
                loadingTrans.GetComponent<TextMeshProUGUI>().text = "Cargando documento...\nPor favor espera";
            }

            if (bgTrans != null)
            {
                RawImage bgImage = bgTrans.GetComponent<RawImage>();
                bgImage.texture = null;
                bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f); // Loading color

                string baseUrl = NetworkConfig.BaseUrl;
                string url = $"{baseUrl}/api/v1/segments/{segmentId}/permits";
                
                Debug.Log("Fetching permits with HttpClient from: " + url);
                
                var fetchTask = Task.Run(async () => {
                    using (HttpClient client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromSeconds(60);
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
                Debug.Log("Permit JSON Received: " + json);
                
                PermitData[] permits = null;
                try {
                    permits = JsonHelper.FromJson<PermitData>(json);
                } catch (Exception) {}

                string foundPdfUrl = "";
                
                if (permits == null || permits.Length == 0 || string.IsNullOrEmpty(permits[0].pdfUrl))
                {
                    Debug.LogWarning("[PDF DEBUG] JsonUtility falló o devolvió vacío. Intentando extracción manual...");
                    int pdfIndex = json.IndexOf("\"pdfUrl\":\"", StringComparison.Ordinal);
                    if (pdfIndex > 0)
                    {
                        pdfIndex += 10;
                        int endQuote = json.IndexOf("\"", pdfIndex, StringComparison.Ordinal);
                        if (endQuote > pdfIndex)
                        {
                            foundPdfUrl = json.Substring(pdfIndex, endQuote - pdfIndex);
                            Debug.Log($"[PDF DEBUG] Extracción manual exitosa: {foundPdfUrl}");
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
                                permits = new PermitData[] { new PermitData() { pdfUrl = foundPdfUrl, previewImageUrl = imgUrl } };
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
                    
                    if (string.IsNullOrEmpty(foundPdfUrl) && !string.IsNullOrEmpty(permits[0].pdfUrl))
                    {
                        foundPdfUrl = permits[0].pdfUrl;
                    }
                    
                    if (!string.IsNullOrEmpty(foundPdfUrl))
                    {
                        foundPdfUrl = NetworkConfig.ResolveUrl(foundPdfUrl);
                    }
                    if (!string.IsNullOrEmpty(foundPdfUrl))
                    {
                        foundPdfUrl = Uri.EscapeUriString(foundPdfUrl);
                        onPdfReady?.Invoke(foundPdfUrl);
                    }

                    if (!string.IsNullOrEmpty(imgUrl))
                    {
                        imgUrl = NetworkConfig.ResolveUrl(imgUrl);
                        Debug.Log("Preview Image URL after replace: " + imgUrl);
                        imgUrl = Uri.EscapeUriString(imgUrl);
                        // Start coroutine to load image (in MonoBehaviour)
                        var dummyBehaviour = permitPopup.GetComponentInParent<MonoBehaviour>();
                        if (dummyBehaviour != null)
                        {
                            dummyBehaviour.StartCoroutine(ImageNetworkLoader.LoadImageFromUrl(imgUrl, bgImage, loadingTrans));
                        }
                    }
                    else 
                    {
                        Debug.LogWarning("imgUrl is empty or null");
                        if (loadingTrans != null) loadingTrans.GetComponent<TextMeshProUGUI>().text = "No hay previsualización disponible";
                    }
                }
                else 
                {
                    Debug.LogWarning("Parsed permits array is empty or null.");
                    if (loadingTrans != null) loadingTrans.GetComponent<TextMeshProUGUI>().text = "No se encontraron permisos";
                }
            }
        }
    }
}
