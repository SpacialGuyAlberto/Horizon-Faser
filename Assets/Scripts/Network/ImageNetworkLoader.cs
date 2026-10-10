using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Threading.Tasks;
using System.Net.Http;
using System;

namespace HorizonFaser.Network
{
    public static class ImageNetworkLoader
    {
        public static IEnumerator LoadImageFromUrl(string url, RawImage rawImage, Transform loadingTrans = null)
        {
            Debug.Log("Downloading texture with HttpClient from: " + url);
            
            var fetchTask = Task.Run(async () => {
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(60);
                    var response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsByteArrayAsync();
                }
            });

            while (!fetchTask.IsCompleted)
            {
                yield return null;
            }

            if (fetchTask.IsFaulted)
            {
                Debug.LogError("Failed to download texture: " + fetchTask.Exception?.Message);
                if (loadingTrans != null)
                {
                    var textMesh = loadingTrans.GetComponent<TextMeshProUGUI>();
                    if (textMesh != null)
                        textMesh.text = "Error al descargar imagen:\n" + fetchTask.Exception?.GetBaseException().Message;
                }
                yield break;
            }

            byte[] imageBytes = fetchTask.Result;
            if (imageBytes != null && imageBytes.Length > 0)
            {
                // Must be executed on the main thread
                Texture2D texture = new Texture2D(2, 2);
                if (texture.LoadImage(imageBytes))
                {
                    if (rawImage != null)
                    {
                        rawImage.texture = texture;
                        rawImage.color = Color.white;
                    }
                    Debug.Log("Texture applied successfully via HttpClient.");
                    if (loadingTrans != null) loadingTrans.gameObject.SetActive(false);
                }
                else
                {
                    Debug.LogError("Texture2D.LoadImage failed to parse bytes!");
                    if (loadingTrans != null)
                    {
                        var textMesh = loadingTrans.GetComponent<TextMeshProUGUI>();
                        if (textMesh != null) textMesh.text = "Error: Formato de imagen inválido";
                    }
                }
            }
            else
            {
                if (loadingTrans != null)
                {
                    var textMesh = loadingTrans.GetComponent<TextMeshProUGUI>();
                    if (textMesh != null) textMesh.text = "Error: Imagen vacía";
                }
            }
        }
    }
}
