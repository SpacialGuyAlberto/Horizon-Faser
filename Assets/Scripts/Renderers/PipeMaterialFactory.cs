using System.Collections.Generic;
using UnityEngine;

namespace HorizonFaser.Renderers
{
    /// <summary>
    /// Crea y cachea materiales URP pensados para ver tuberías a distancia en AR
    /// sin "pixeles intermitentes" (shimmering / specular aliasing).
    /// - Un único material compartido por tipo de tubería (mejor batching y FPS).
    /// - Filtrado trilinear + anisotrópico y un pequeño mip bias para que el detalle
    ///   fino (corrugado) no "chispee" a distancia.
    /// - Smoothness baja y normal map suavizado para evitar destellos especulares
    ///   en geometría muy delgada.
    /// </summary>
    public static class PipeMaterialFactory
    {
        private static readonly Dictionary<string, Material> _cache = new Dictionary<string, Material>();

        private const int AnisoLevel = 8;
        private const float AlbedoMipBias = 0.5f;
        private const float NormalMipBias = 1.0f;
        private const float NormalStrength = 0.4f;

        public static Material Get(string basePath, Color fallbackColor, float metallic, float smoothness, bool useMetallicMap = false)
        {
            string key = $"{basePath}|{fallbackColor}|{metallic}|{smoothness}|{useMetallicMap}";
            if (_cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));

            Texture2D tex = string.IsNullOrEmpty(basePath) ? null : Resources.Load<Texture2D>(basePath);
            Texture2D normalTex = string.IsNullOrEmpty(basePath) ? null : Resources.Load<Texture2D>(basePath + "_normal");
            Texture2D metallicTex = (useMetallicMap && !string.IsNullOrEmpty(basePath)) ? Resources.Load<Texture2D>(basePath + "_metallic") : null;

            if (tex != null)
            {
                ConfigureSampler(tex, AlbedoMipBias);
                mat.mainTexture = tex;
            }
            else
            {
                mat.color = fallbackColor;
            }

            if (normalTex != null)
            {
                ConfigureSampler(normalTex, NormalMipBias);
                mat.SetTexture("_BumpMap", normalTex);
                mat.SetFloat("_BumpScale", NormalStrength);
                mat.EnableKeyword("_NORMALMAP");
            }

            if (metallicTex != null)
            {
                ConfigureSampler(metallicTex, AlbedoMipBias);
                mat.SetTexture("_MetallicGlossMap", metallicTex);
                mat.EnableKeyword("_METALLICGLOSSMAP");
            }

            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            mat.enableInstancing = true;

            _cache[key] = mat;
            return mat;
        }

        private static void ConfigureSampler(Texture2D t, float mipBias)
        {
            t.filterMode = FilterMode.Trilinear;
            t.anisoLevel = AnisoLevel;
            t.mipMapBias = mipBias;
        }
    }
}
